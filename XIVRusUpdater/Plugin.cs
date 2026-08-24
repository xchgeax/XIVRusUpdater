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

namespace XIVRusUpdater;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
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
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private DownloadWindow DownloadWindow { get; init; }
    private ChangelogWindow Changelog { get; init; }

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

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(DownloadWindow);
        WindowSystem.AddWindow(Changelog);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
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

    public static string CurrentGameVersion => DataManager.GameData.Repositories["ffxiv"].Version;

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
        MainWindow.Toggle();
    }

    private async void OnUpdate(IFramework framework)
    {
        if (DateTime.Now > nextRefresh)
        {
            nextRefresh = DateTime.Now.AddMinutes(Configuration.UpdateCheckIntervalMinutes);
            Plugin.Log.Information($"Perform timed update... Next update: {nextRefresh.ToString()}");

            _ = networkService.CheckForUpdates();
        }

        DownloadWindow.IsOpen = State.Download.Any(x => x.IsDownloading);
        Changelog.IsOpen = State.Translation.ShowChangelog || State.Penumbra.ShowChangelog;
    }

    private void OnLanguageChanged(string lang)
    {
        InitLocalization();
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
