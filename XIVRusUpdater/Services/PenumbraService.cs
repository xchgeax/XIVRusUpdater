using Dalamud.Plugin;
using Dalamud.Plugin.Ipc.Exceptions;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using Penumbra.Api.IpcSubscribers;
using Penumbra.Api.IpcSubscribers.Legacy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XIVRusUpdater.Services;

public sealed class PenumbraService
{
    private Penumbra.Api.IpcSubscribers.GetModList GetListMod { get; } = null!;
    private Penumbra.Api.IpcSubscribers.InstallMod InstallMod { get; } = null!;
    private Penumbra.Api.IpcSubscribers.DeleteMod DeleteMod { get; } = null!;
    private Penumbra.Api.IpcSubscribers.ReloadMod ReloadMod { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetEnabledState GetEnableStatus { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetModListAdapter ModList { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetCollection GetCollection { get; } = null!;
    private Penumbra.Api.IpcSubscribers.TrySetMod ChangeEnabled { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetCurrentModSettings ModSettings { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetModDirectory GetDirectory { get; } = null!;
    private EventSubscriber Init { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetCollections GetCollections { get; } = null!;

    public PenumbraService(IDalamudPluginInterface @interface)
    {
        GetListMod = new (@interface);
        InstallMod = new (@interface);
        DeleteMod = new (@interface);
        ReloadMod = new (@interface);
        GetEnableStatus = new (@interface);
        ModList = new (@interface);
        GetCollection = new (@interface);
        ChangeEnabled = new (@interface);
        ModSettings = new (@interface);
        GetDirectory = new (@interface);
        GetCollections = new (@interface);

        Init = Initialized.Subscriber(@interface, OnPenumbraInitialized);
        Init.Enable();
        try
        {
            if (IsEnabled())
                OnPenumbraInitialized();
        }
        catch(IpcNotReadyError)
        {
            // Do nothing, OnPenumbraInitialized do all works
        }
    }

    private void OnPenumbraInitialized()
    {
        Plugin.Log.Debug("Penumbra initialized... Perform checks");
        _ = CheckAndDisableIfNeeded();
        _ = Plugin.networkService.CheckForUpdates();
    }

    public async Task CheckAndDisableIfNeeded()
    {
        var remoteVersion = await Plugin.networkService.GetBranchStatus();
        if (remoteVersion is null) return;

        var localVersion = Plugin.CurrentGameVersion;

        if (remoteVersion.GameVersion == localVersion)
            return;

        Plugin.Log.Information("Disabling XIV Rus due GameVersion missmatch");
        Plugin.State.mod.Enabled = false;
    }

    public bool IsEnabled() => GetEnableStatus.Invoke();
    
    private Guid? GetDefaultCollection()
    {
        return GetCollection.Invoke(Penumbra.Api.Enums.ApiCollectionType.Default)?.Id;
    }

    public string GetDefaultDirectory() => GetDirectory.Invoke();

    public bool IsModInstalled(string modName) => GetListMod.Invoke().ContainsKey(modName);
    
    public string? GetModVersion(string modName)
    {
        if (!IsModInstalled(modName)) return null;

        var modList = ModList.Invoke();
        var mod = modList.FirstOrDefault(x => x.Identifier == modName);
        var modVersion = mod.Version;
        return modVersion;
    }

    public DirectoryInfo? GetModPath(string modName)
    {
        if (!IsModInstalled(modName)) return null;

        var modList = ModList.Invoke();
        var mod = modList.FirstOrDefault(x => x.Identifier == modName);
        return mod.ModPath;
    }

    public bool IsModEnabled(string modName)
    {
        var collectionId = GetDefaultCollection();
        if (collectionId is null) return false;

        var modSettings = ModSettings.Invoke(collectionId.Value, string.Empty, modName);

        // TODO: Add logs here
        switch(modSettings.Item1)
        {
            case Penumbra.Api.Enums.PenumbraApiEc.Success:
                return modSettings.Item2?.Item1 ?? false;
            case Penumbra.Api.Enums.PenumbraApiEc.InvalidArgument:
                break;
            case Penumbra.Api.Enums.PenumbraApiEc.CollectionMissing:
                break;
            case Penumbra.Api.Enums.PenumbraApiEc.ModMissing:
                break;
        }

        return false;
    }

    public bool SetModEnabled(string modName, bool enabled)
    {
        var collections = GetCollections.Invoke();
        var anyFailed = false;

        foreach (var (collectionId, _) in collections)
        {
            var response = ChangeEnabled.Invoke(collectionId, string.Empty, enabled, modName);
            if (response is not PenumbraApiEc.Success and not PenumbraApiEc.NothingChanged)
                anyFailed = true;
        }

        return !anyFailed;
    }

    public bool DeleteMods(string modName)
    {
        var responce = DeleteMod.Invoke(string.Empty, modName);

        if (responce == Penumbra.Api.Enums.PenumbraApiEc.Success) return true;
        else return false;
    }

    public bool ReloadMods(string modName)
    {
        var response = ReloadMod.Invoke(string.Empty, modName);

        if (response == Penumbra.Api.Enums.PenumbraApiEc.Success) return true;
        else return false;
    }

    public bool InstallMods(string downloadPath)
    {
        var response = InstallMod.Invoke(downloadPath);

        if (response == Penumbra.Api.Enums.PenumbraApiEc.Success) return true;
        else return false;
    }
}
