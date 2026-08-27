using CheapLoc;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using XIVRusUpdater.Core;
using XIVRusUpdater.Hooks;
using XIVRusUpdater.Services;
using XIVRusUpdater.Utils.States;
using XIVRusUpdater.Windows;
using XIVRusUpdater.Windows.Debug;

namespace XIVRusUpdater;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider interopProvider { get; private set; } = null!;

    public static EXDHooks HookLayers { get; private set; } = null!;
    public static TranslationFilter filter { get; private set; } = null!;

    internal static PenumbraService PenumbraApi { get; private set; } = null!;
    internal static NetworkService networkService { get; private set; } = null!;
    internal static UpdaterState State { get; private set; } = null!;

    private const string CommandName = "/xivrus";
    
    public Configuration Configuration { get; init; }
    private DateTime nextRefresh = DateTime.MinValue;


    public readonly WindowSystem WindowSystem = new("XIV Rus Updater");
    private readonly ConfigWindow ConfigWindow;
    private readonly MainWindow MainWindow;
    private readonly DownloadWindow DownloadWindow;
    private readonly ChangelogWindow Changelog;
    private readonly DebugWindow Debug;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        filter = new TranslationFilter();
        filter.Rebuild(Configuration.DisabledComponents);
        HookLayers = new EXDHooks(interopProvider, Configuration.EngineId);
        State = new UpdaterState();
        networkService = new NetworkService(this);
        PenumbraApi = new PenumbraService(PluginInterface);
        _ = Task.Run(Initialization);

        Framework.Update += OnUpdate;
        
        var iconPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "icon.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, iconPath);
        DownloadWindow = new DownloadWindow();
        Changelog = new ChangelogWindow(this);
        Debug = new DebugWindow();

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(DownloadWindow);
        WindowSystem.AddWindow(Changelog);
        WindowSystem.AddWindow(Debug);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the plugin window. Use '/xivrus cache' for cache memory usage or '/xivrus sheetcache' to show the EXD sheet schema cache."
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        PluginInterface.LanguageChanged += OnLanguageChanged;
    }

    public async Task Initialization()
    {
        InitLocalization();
    }

    public void InitLocalization()
    {
        var lang = PluginInterface.UiLanguage;

        var path = Path.Combine(PluginInterface.AssemblyLocation.Directory!.FullName, $"lang/{lang}.json");

        if (!File.Exists(path))
        {
            Loc.SetupWithFallbacks(Assembly.GetExecutingAssembly());
            return;
        }

        var json = File.ReadAllText(path);

        Loc.Setup(json, Assembly.GetExecutingAssembly());
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.LanguageChanged -= OnLanguageChanged;

        WindowSystem.RemoveAllWindows();

        HookLayers.Dispose();
        ConfigWindow.Dispose();
        MainWindow.Dispose();
        DownloadWindow.Dispose();
        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        if (string.Equals(args.Trim(), "debug", StringComparison.OrdinalIgnoreCase))
        { 
            Debug.Toggle();
            return;
        }
        else if (string.Equals(args.Trim(), "cache", StringComparison.OrdinalIgnoreCase))
        {
            PrintCacheMemoryUsage();
            return;
        }
        else if (string.Equals(args.Trim(), "sheetcache", StringComparison.OrdinalIgnoreCase))
        {
            PrintSheetSchemaCache();
            return;
        }

        MainWindow.Toggle();
    }

    private static void PrintCacheMemoryUsage()
    {
        var translation = HookLayers.parser.GetCacheMemoryStats();
        var nativeMemory = translation.TotalNativeMemoryBytes;
        var columnCacheCount = HookLayers.ColumnCacheCount;
        var stringColumnCacheCount = HookLayers.StringColumnCacheCount;

        ChatGui.Print(
            $"[XIV Rus] Translation caches: {FormatBytes(nativeMemory)} native payload " +
            $"({translation.ActiveResourceCount} active resources, " +
            $"{translation.RetiredResourceCount} retired resources in " +
            $"{translation.RetiredManagerCount} manager(s)).");
        ChatGui.Print(
            $"[XIV Rus] Retained retired translation cache: {FormatBytes(translation.RetiredNativeMemoryBytes)} " +
            $"({translation.RetiredResourceCount} resources, {translation.RetiredManagerCount} manager(s)).");
        ChatGui.Print(
            $"[XIV Rus] EXD column lookup cache: {columnCacheCount:N0} entries " +
            "(game column address -> sheet, row, and column metadata).");
        ChatGui.Print(
            $"[XIV Rus] EXD sheet schema cache: {stringColumnCacheCount:N0} entries " +
            "(game sheet name -> global indexes of string columns).");
        ChatGui.Print("[XIV Rus] Reported memory is unmanaged translation-buffer payload; object overhead is not included.");
    }

    private static void PrintSheetSchemaCache()
    {
        var entries = HookLayers.GetStringColumnIndicesCacheSnapshot()
            .OrderBy(entry => entry.Key, StringComparer.Ordinal);

        ChatGui.Print($"[XIV Rus] EXD sheet schema cache ({HookLayers.StringColumnCacheCount:N0} entries):");

        foreach (var entry in entries)
        {
            ChatGui.Print($"[XIV Rus] {entry.Key}: [{string.Join(", ", entry.Value)}]");
        }
    }

    private static string FormatBytes(long bytes)
    {
        const double megabyte = 1024d * 1024d;
        return $"{bytes:N0} B / {bytes / megabyte:N2} MiB";
    }

    private async void OnUpdate(IFramework framework)
    {
        if (DateTime.Now > nextRefresh)
        {
            nextRefresh = DateTime.Now.AddMinutes(Configuration.UpdateCheckIntervalMinutes);
            Plugin.Log.Information($"Perform timed update... Next update: {nextRefresh.ToString()}");

            _ = networkService.CheckForUpdates();
        }

        DownloadWindow.IsOpen = State.Penumbra.Download.IsDownloading || State.Translation.Download.IsDownloading;
        Changelog.IsOpen = State.ShowChangelog;
    }

    private void OnLanguageChanged(string lang)
    {
        InitLocalization();
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
