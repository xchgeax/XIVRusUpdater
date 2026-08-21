using System;
using XIVRusUpdater.Core.Components;
using XIVRusUpdater.Models;
using XIVRusUpdater.Services;

namespace XIVRusUpdater.Utils.States;

public sealed class UpdaterState
{
    public DownloadState Download { get; set; } = new DownloadState();

    public TranslationManifest? LastRemoteStatus { get; set; }

    public NetworkService.AvailabilityStatus Availability { get; set; }
        = NetworkService.AvailabilityStatus.Disabled;

    public bool PenumbraEnabled { get; set; }

    public bool ModInstalled { get; set; }

    public string? InstalledVersion { get; set; }

    public string? RemoteVersion { get; set; }

    public bool UpdateAvailable { get; set; }

    public DateTime LastCheck { get; set; }

    public string? LastError { get; set; }

    public bool ShowChangelog { get; set; }

    public string? LastChangelog { get; set; }
}
