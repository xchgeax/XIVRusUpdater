using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XIVRusUpdater;
using XIVRusUpdater.Utils;
using XIVRusUpdater.Utils.States;

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
        var download = Plugin.State.Penumbra.Download;

        if (download.IsDownloading) DrawDownloadInfo(download);
        
        download = Plugin.State.Translation.Download;

        if (download.IsDownloading) DrawDownloadInfo(download);
    }

    private void DrawDownloadInfo(DownloadState download)
    {
        ImGui.TextWrapped(download.FileName);

        ImGui.ProgressBar(
            download.Progress,
            new Vector2(-1, 24));

        ImGui.Spacing();

        ImGui.Text(string.Format(Translations.DownloadProgress,
            Math.Round(download.DownloadedBytes / 1024f / 1024f),
            Math.Round(download.TotalBytes / 1024f / 1024f)));

        ImGui.Text(Translations.DownloadSource);
        ImGui.SameLine();
        ImGui.TextWrapped(download.CurrentSource);

        ImGui.Text(string.Format(Translations.DownloadSpeed, Math.Round(download.SpeedMBps, 2)));
    }
}
