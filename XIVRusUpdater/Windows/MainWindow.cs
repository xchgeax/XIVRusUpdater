using CheapLoc;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using XIVRusUpdater.Services;
using XIVRusUpdater.Utils;
using XIVRusUpdater.Utils.States;
using XIVRusUpdater.Windows.Dialogs;

namespace XIVRusUpdater.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly string LogoImagePath;
    private readonly Plugin plugin;
    private Task? refreshTask;
    private Task? downloadTask;
    
    private enum OverallStatus
    {
        Ok,
        UpdateAvailable,
        Warning,
        Disabled,
        Error
    }

    public MainWindow(Plugin plugin, string logoImagePath)
        : base($"{Translations.MainWindowTitle}###XIVMain")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        LogoImagePath = logoImagePath;
        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var state = Plugin.State;

        DrawStatusBanner();

        ImGui.Spacing();

        if (ImGui.CollapsingHeader(Translations.SystemStatusHeader, ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.BulletText(string.Format(Translations.PenumbraStatus, state.PenumbraEnabled ? "Enabled" : "Disabled"));

            ImGui.Separator();

            ImGui.BulletText(string.Format(Translations.XIVRusStatus, state.Translation.Installed ? "Installed" : "Not Installed"));

            ImGui.BulletText(string.Format(Translations.VersionStatus, state.Translation.RemoteVersion));

            ImGui.Separator();

            ImGui.BulletText(string.Format(Translations.XIVRusStatus, state.Penumbra.Installed ? "Installed" : "Not Installed"));

            ImGui.BulletText(string.Format(Translations.VersionStatus, state.Penumbra.RemoteVersion));
        }

        if (ImGui.CollapsingHeader(Translations.VersionHeader, ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text(string.Format(Translations.InstalledVersion, state.Translation.Version));
            ImGui.Text(string.Format(Translations.InstalledVersion, state.Penumbra.Version));

            ImGui.Text(Translations.LastCheck);
            ImGui.SameLine();
            ImGui.TextDisabled(plugin.Configuration.LastUpdateCheck == default ? "Never"
                    : plugin.Configuration.LastUpdateCheck.ToString("G"));
        }

        if (ImGui.CollapsingHeader(Translations.ChangelogHeader, ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextWrapped(Plugin.State.LastChangelog ?? Translations.NoChangelog);
        }

        if (ImGui.CollapsingHeader(Translations.ActionsHeader, ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (ImGui.Button(Translations.RefreshButton, new Vector2(-1, 0)))
            {
                refreshTask ??= Plugin.networkService.CheckForUpdates();
            }

            if (refreshTask?.IsCompleted == true)
            {
                refreshTask = null;
            }

            bool updateAvailable = Plugin.State.UpdateAvailable;

            using (ImRaii.Disabled(!updateAvailable))
            {
                if (ImGui.Button(Translations.UpdateButton, new Vector2(-1, 0)))
                {
                    downloadTask ??= Plugin.networkService.DownloadLatestModAsync();
                }
            }

            if(downloadTask?.IsCompleted == true)
            {
                downloadTask = null;
            }

            if (ImGui.Button(Translations.OpenConfigButton, new Vector2(-1, 0)))
            {
                plugin.ToggleConfigUi();
            }
        }

        if (ImGui.CollapsingHeader(Translations.DiagnosticsHeader))
        {
            ImGui.TextDisabled(Translations.Branch);
            ImGui.SameLine();
            ImGui.Text(plugin.Configuration.Channel.ToString());

            ImGui.TextDisabled(Translations.TesterAllowance);
            ImGui.SameLine();

            if (!plugin.Configuration.TesterHumanCheck)
                ImGui.TextColored(ImGuiColors.DalamudYellow, Translations.TesterDenied);
            else
                ImGui.TextColored(ImGuiColors.HealerGreen, Translations.TesterAllowed);
        }
    }

    private OverallStatus GetOverallStatus()
    {
        var state = Plugin.State;

        if (!state.PenumbraEnabled)
            return OverallStatus.Error;

        if (Plugin.State.UpdateAvailable)
            return OverallStatus.UpdateAvailable;

        return OverallStatus.Ok;
    }

    private void DrawStatusBanner()
    {
        var status = GetOverallStatus();
        
        Vector4 color;
        string text;

        switch (status)
        {
            case OverallStatus.Ok:
                color = ImGuiColors.HealerGreen;
                text = Translations.StatusUpToDate;
                break;

            case OverallStatus.UpdateAvailable:
                color = ImGuiColors.DalamudYellow;
                text = Translations.StatusUpdateAvailable;
                break;

            case OverallStatus.Disabled:
                color = ImGuiColors.DalamudRed;
                text = Translations.StatusDisabled;
                break;

            default:
                color = ImGuiColors.DalamudRed;
                text = Translations.StatusError;
                break;
        }

        using (ImRaii.PushColor(ImGuiCol.ChildBg, color * new Vector4(1, 1, 1, 0.15f)))
        {
            ImGui.BeginChild("StatusBanner", new Vector2(-1, 50), true);

            using (ImRaii.PushColor(ImGuiCol.Text, color))
            {
                ImGui.SetCursorPosY(15);
                ImGui.TextUnformatted(text);
            }

            ImGui.EndChild();
        }
    }
}
