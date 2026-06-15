using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XIVRusUpdater;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Windows;

public class DownloadWindow : Window
{
    public DownloadWindow()
        : base($"{Translations.DownloadTitle}###DownloadWindow")
    {
        RespectCloseHotkey = false;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var download = Plugin.State.Download;

        ImGui.TextWrapped(download.FileName);

        ImGui.ProgressBar(
            download.Progress,
            new Vector2(-1, 24));

        ImGui.Spacing();

        ImGui.Text(string.Format(Translations.DownloadTitle, download.DownloadedBytes / 1024f / 1024f, download.TotalBytes / 1024f / 1024f));

        ImGui.TextWrapped(string.Format(Translations.DownloadSource, download.CurrentSource));

        ImGui.Text(string.Format(Translations.DownloadSpeed, Math.Round(download.SpeedMBps, 2)));
    }
}
