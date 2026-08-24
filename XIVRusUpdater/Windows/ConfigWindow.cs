using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;
using XIVRusUpdater.Core;
using XIVRusUpdater.Core.Components;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    public ConfigWindow(Plugin plugin) : base($"{Translations.ConfigWindowTitle}###XIVConfig")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.CollapsingHeader(Translations.GeneralHeader, ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.BeginChild("GeneralSettings", new Vector2(0, 70), true);

            var currentEngine = TranslationEngines.Get(configuration.EngineId);

            if (ImGui.BeginCombo("Translation Engine", currentEngine?.DisplayName ?? "Unknown"))
            {
                foreach (var engine in TranslationEngines.All)
                {
                    bool selected = engine.Id == configuration.EngineId;

                    if (ImGui.Selectable(engine.DisplayName, selected))
                    {
                        configuration.EngineId = engine.Id;
                        configuration.Save();
                    }

                    if (selected)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            var showNotify = configuration.ShowNotifications;
            var showChangelog = configuration.ShowChangelogAfterUpdate;

            if (ImGui.Checkbox(Translations.ShowNotifications, ref showNotify))
            {
                configuration.ShowNotifications = showNotify;
                configuration.Save();
            }
            if (ImGui.Checkbox(Translations.ShowChangelogAfterUpdate, ref showChangelog))
            {
                configuration.ShowChangelogAfterUpdate = showChangelog;
                configuration.Save();
            }

            ImGui.EndChild();
        }

        if (ImGui.CollapsingHeader("Components"))
        {
            var translationFilter = Plugin.filter;

            if (ImGui.Button("Enable All"))
            {
                configuration.DisabledComponents.Clear();
                translationFilter.Rebuild(configuration.DisabledComponents);
            }

            ImGui.SameLine();

            if (ImGui.Button("Disable All"))
            {
                configuration.DisabledComponents.Clear();

                foreach (var component in TranslationComponents.All)
                    configuration.DisabledComponents.Add(component.Id);

                translationFilter.Rebuild(configuration.DisabledComponents);
            }

            ImGui.Separator();

            foreach (var component in TranslationComponents.All)
            {
                bool enabled = !configuration.DisabledComponents.Contains(component.Id);

                if (ImGui.Checkbox(component.DisplayName, ref enabled))
                {
                    if (enabled)
                        configuration.DisabledComponents.Remove(component.Id);
                    else
                        configuration.DisabledComponents.Add(component.Id);

                    translationFilter.Rebuild(configuration.DisabledComponents);
                }

                ImGui.SameLine();

                ImGuiComponents.HelpMarker(component.Description);
            }
        }

        if (ImGui.CollapsingHeader(Translations.UpdatesHeader, ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.BeginChild("UpdateSettings", new Vector2(0, 180), true);

            ImGui.Spacing();

            int interval = configuration.UpdateCheckIntervalMinutes;
            if(ImGui.SliderInt(Translations.CheckInterval, ref interval, 5, 1440))
            {
                configuration.UpdateCheckIntervalMinutes = interval;
                configuration.Save();
            }

            ImGui.Spacing();

            var autoDownload = configuration.AutoDownloadUpdates;
            var autoInstall = configuration.AutoInstallUpdates;
            
            if(ImGui.Checkbox(Translations.AutoDownloadUpdates, ref autoDownload))
            {
                configuration.AutoDownloadUpdates = autoDownload;
                configuration.Save();
            }
            if(ImGui.Checkbox(Translations.AutoInstallUpdates, ref autoInstall))
            {
                configuration.AutoInstallUpdates = autoInstall;
                configuration.Save();
            }

            ImGui.EndChild();
        }

        if (ImGui.CollapsingHeader(Translations.TesterHeader))
        {
            ImGui.BeginChild("TesterSettings", new Vector2(0, 120), true);

            if (ImGui.BeginCombo(Translations.TesterChannel, configuration.Channel.ToString()))
            {
                foreach (var channel in Enum.GetValues<UpdateChannel>())
                {
                    bool isTestChannel = channel != UpdateChannel.Stable;

                    if (isTestChannel && !configuration.TesterHumanCheck)
                        continue;

                    bool selected = channel == configuration.Channel;

                    if (ImGui.Selectable(channel.ToString(), selected))
                    {
                        configuration.Channel = channel;
                        configuration.Save();
                    }

                    if (selected) ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.Separator();

            bool testerHumanCheck = configuration.TesterHumanCheck;

            ImGui.TextWrapped(Translations.TesterWarning);

            if (ImGui.Checkbox(Translations.TesterAgreement, ref testerHumanCheck))
            {
                configuration.TesterHumanCheck = testerHumanCheck;
                configuration.Save();
            }

            ImGui.EndChild();
        }

        if (ImGui.CollapsingHeader(Translations.InformationHeader))
        {
            ImGui.BeginChild("InformationPanel", new Vector2(0, 140), true);

            ImGui.Text(Translations.InstalledVersionLabel);
            ImGui.SameLine();
            ImGui.TextDisabled(configuration.LastInstalledVersion);

            ImGui.Text(Translations.LatestVersionLabel);
            ImGui.SameLine();
            ImGui.TextDisabled(configuration.LastKnownRemoteVersion);

            ImGui.Text(Translations.InstalledVersionLabel);
            ImGui.SameLine();
            ImGui.TextDisabled(configuration.LastInstalledPenumbra);

            ImGui.Text(Translations.LatestVersionLabel);
            ImGui.SameLine();
            ImGui.TextDisabled(configuration.LastKnownRemotePenumbra);

            ImGui.Text(Translations.LastUpdateCheckLabel);
            ImGui.SameLine();
            ImGui.TextDisabled(configuration.LastUpdateCheck.ToString("g"));

            ImGui.Text(Translations.LastSuccessfulUpdateLabel);
            ImGui.SameLine();
            ImGui.TextDisabled(configuration.LastSuccessfulUpdate.ToString("g"));

            ImGui.EndChild();
        }
    }
}
