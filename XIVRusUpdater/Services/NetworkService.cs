using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using XIVRusUpdater.Core.Components;
using XIVRusUpdater.Models;
using XIVRusUpdater.Utils.Extentions;
using static XIVRusUpdater.Utils.Extentions.HttpClientProgressExtensions;

namespace XIVRusUpdater.Services;

public class NetworkService
{
    private static readonly HttpClient Client = CreateClient();
    
    private readonly Plugin plugin;
    
    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd($"XIVRusUpdater/{Plugin.PluginInterface.Manifest.AssemblyVersion}");

        return client;
    }

    public string CurrentBranch()
    {
        var engine = TranslationEngines.Get(plugin.Configuration.EngineId);

        return plugin.Configuration.Channel == UpdateChannel.Beta ? $"{engine.ApiUrl}/branches/test" : $"{engine.ApiUrl}/branches/release";
    }

    public async Task<TranslationManifest?> GetBranchStatus()
    {
        var branch = CurrentBranch();

        using HttpResponseMessage responseMessage = await Client.GetAsync(branch);

        responseMessage.EnsureSuccessStatusCode();

        var status = JsonConvert.DeserializeObject<TranslationManifest>(await responseMessage.Content.ReadAsStringAsync());

        Plugin.State.LastRemoteStatus = status;
        return status;
    }

    public async Task<string?> GetLastRemoteVersionAsync()
    {
        var response = await GetBranchStatus();
        if (response == null) return null;

        return response.Version;
    }

    public async Task<string?> GetLastRemotePenumbraAsync()
    {
        var response = await GetBranchStatus();
        if (response == null) return null;

        return response.PenumbraVersion;
    }

    public async Task CheckForUpdates()
    {
        Plugin.Log.Information("Update Check started");
        plugin.Configuration.LastUpdateCheck = DateTime.Now;
        
        await RefreshAsync();

        plugin.Configuration.LastSuccessfulUpdate = DateTime.Now;

        if (!Plugin.State.UpdateAvailable)
            return;

        if (!plugin.Configuration.AutoDownloadUpdates)
            return;

        await DownloadLatestModAsync();
        //
    }

    public async Task DownloadLatestModAsync()
    {
        var release = await GetBranchStatus();

        if(release == null) return;

        if(release.Version != null)
            plugin.Configuration.LastInstalledVersion = release.Version;
        
        var downloadSource = await GetFastestSource(release.DownloadUrl);

        if (downloadSource == null)
            return;

        Plugin.Log.Information($"Starting download {downloadSource.FileName} from {downloadSource.Url}...");

        var tempFile = Path.Combine(Plugin.PenumbraApi.GetDefaultDirectory(), downloadSource.FileName);

        var success = await DownloadModAsync(downloadSource.Url, tempFile);

        if (!success)
            return;

        Plugin.Log.Info($"Downloading {downloadSource.FileName} successful complete");

        if (!plugin.Configuration.AutoInstallUpdates)
            return;

        InstallDownloadedVersionAsync(tempFile);
    }

    //TODO: Reimplement for translation download
    public async Task DownloadLatestTranslationAsync()
    {
        var release = await GetBranchStatus();

        if (release == null) return;

        if (release.Version != null)
            plugin.Configuration.LastInstalledVersion = release.Version;

        var downloadSource = await GetFastestSource(release.DownloadUrl);

        if (downloadSource == null)
            return;

        Plugin.Log.Information($"Starting download {downloadSource.FileName} from {downloadSource.Url}...");

        var tempFile = Path.Combine(Plugin.PenumbraApi.GetDefaultDirectory(), downloadSource.FileName);

        var success = await DownloadModAsync(downloadSource.Url, tempFile);

        if (!success)
            return;

        Plugin.Log.Info($"Downloading {downloadSource.FileName} successful complete");

        if (!plugin.Configuration.AutoInstallUpdates)
            return;

        InstallDownloadedVersionAsync(tempFile);
    }

    public void InstallDownloadedVersionAsync(string filePath)
    {
        var engine = TranslationEngines.Get(plugin.Configuration.EngineId);

        Plugin.PenumbraApi.DeleteMod(engine!.ModName);

        bool isInstall = Plugin.PenumbraApi.InstallMod(filePath);
        Plugin.Log.Information($"XIV Rus has been queued for installation in Penumbra. Status: {isInstall}");
    }


    //TODO: Implement unpacking
    public void InstallDownloadedPenumbraAsync(string filePath)
    {
        var engine = TranslationEngines.Get(plugin.Configuration.EngineId);

        Plugin.PenumbraApi.DeleteMod(engine!.ModName);

        bool isInstall = Plugin.PenumbraApi.InstallMod(filePath);
        Plugin.Log.Information($"XIV Rus has been queued for installation in Penumbra. Status: {isInstall}");
    }

    public async Task RefreshAsync()
    {
        try
        {
            var engine = TranslationEngines.Get(plugin.Configuration.EngineId);

            Plugin.State.PenumbraEnabled = Plugin.PenumbraApi.IsPenumbraEnabled();

            var penumbraManifest = Plugin.State.Penumbra;
            var translationManifest = Plugin.State.Translation;

            penumbraManifest.Installed = Plugin.PenumbraApi.IsModInstalled(engine!.ModName);
    
            var remote = await GetLastRemoteVersionAsync() ?? "Unknown";

            plugin.Configuration.LastKnownRemoteVersion = translationManifest.RemoteVersion = remote;

            plugin.Configuration.LastInstalledVersion = Plugin.State.Translation.Version ?? "Not installed";

            remote = await GetLastRemotePenumbraAsync() ?? "Unknown";

            plugin.Configuration.LastKnownRemotePenumbra = penumbraManifest.RemoteVersion = remote;

            plugin.Configuration.LastInstalledPenumbra = Plugin.State.Translation.Version = Plugin.PenumbraApi.GetModVersion(engine.ModName) ?? "Not installed";            
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Refresh failed");
        }
    }

    private static async Task<DownloadSourceInfo?> GetFastestSource(IEnumerable<string> sources)
    {
        var tasks = sources.Select(async source =>
        {
            var stopwatch = new Stopwatch();
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, source);
                request.Headers.Range = new RangeHeaderValue(0, 10 * 1024 * 1024);

                stopwatch.Start();
                using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if(!response.IsSuccessStatusCode)
                    return (Url: source, FileName: string.Empty, SpeedMbps: 0.0, Success: false);

                byte[] content = await response.Content.ReadAsByteArrayAsync();
                stopwatch.Stop();

                double seconds = stopwatch.Elapsed.TotalSeconds;
                double speedMBps = (content.Length / (1024*1024)) / seconds;

                string? fileName = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName;

                if (string.IsNullOrWhiteSpace(fileName))
                    fileName = Path.GetFileName(new Uri(source).AbsolutePath);

                fileName = fileName?.Trim('"') ?? string.Empty;

                Plugin.Log.Debug($"Download link ({source}) checked. Speed: {speedMBps} MB/s");
                return (Url: source, FileName: fileName, SpeedMbps: speedMBps, Success: true);
            }
            catch
            {
                return (Url: source, FileName: string.Empty, SpeedMbps: 0.0, Success: false);
            }
        });

        var results = await Task.WhenAll(tasks);

        var bestSource = results.Where(x => x.Success).OrderByDescending(x => x.SpeedMbps).FirstOrDefault();

        if (!bestSource.Success)
            return null;

        Plugin.Log.Debug($"Best download source picked. Url: {bestSource.Url}, Speed: {bestSource.SpeedMbps}");

        return new DownloadSourceInfo
        {
            Url = bestSource.Url,
            FileName = bestSource.FileName
        };
    }

    public NetworkService(Plugin pluginRef)
    {
        plugin = pluginRef;
    }

    public async Task<bool> DownloadModAsync(string url, string targetFile)
    {
        var state = Plugin.State.Penumbra.Download;

        state.IsDownloading = true;
        state.CurrentSource = url;
        state.Error = null;
        state.FileName = Path.GetFileName(targetFile);
        state.DownloadedBytes = 0;
        state.TotalBytes = 0;
        state.SpeedMBps = 0;

        try
        {
            var progress = new Progress<DownloadProgressInfo>(p =>
            {
                state.DownloadedBytes = p.DownloadedBytes;
                state.TotalBytes = p.TotalBytes;
                state.SpeedMBps = p.SpeedMBps;
            });

            await using var file = File.Create(targetFile);

            await Client.DownloadDataAsync(url, file, progress);

            return true;
        }
        catch (Exception ex)
        {
            state.Error = ex.Message;
            Plugin.Log.Error($"Download Error: {ex.Message}");
            return false;
        }
        finally
        {
            state.IsDownloading = false;
        }
    }

    public sealed class DownloadSourceInfo
    {
        public string Url { get; init; } = string.Empty;

        public string FileName { get; init; } = string.Empty;
    }
}
