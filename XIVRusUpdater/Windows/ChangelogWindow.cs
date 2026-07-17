using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XIVRusUpdater;
using XIVRusUpdater.Utils;
using XIVRusUpdater.Windows.Dialogs;

namespace XIVRusUpdater.Windows;

public sealed class ChangelogWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    private readonly ConfirmationPopup reloadPopup = new ConfirmationPopup("ReloadPopup");

    public ChangelogWindow(Plugin plugin)
        : base($"{Translations.ChangelogWindowTitle}###XIVRusChangelog")
    {
        Flags = ImGuiWindowFlags.NoCollapse;
        RespectCloseHotkey = false;
        this.plugin = plugin;

        Size = new Vector2(750, 600);
        SizeCondition = ImGuiCond.FirstUseEver;

        RespectCloseHotkey = false;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        ImGui.TextUnformatted(Translations.ChangelogUpdated);
        ImGui.Separator();

        var contentHeight = ImGui.GetContentRegionAvail().Y - 50;

        ImGui.BeginChild("##changelog", new Vector2(0, contentHeight), true);

        string markdown = Plugin.State.LastChangelog ?? Translations.ChangelogUnavailable;

        ImGui.TextWrapped(markdown);

        ImGui.EndChild();

        ImGui.Spacing();

        if (ImGui.Button(Translations.AcceptButton, new Vector2(180, 0)))
        {
            Plugin.State.ShowChangelog = false;
        }

        ImGui.SameLine();

        if (ImGui.Button(Translations.AcceptAndRestartButton, new Vector2(180, 0)))
        {
            reloadPopup.Open();
        }

        reloadPopup.Draw();
    }
}
