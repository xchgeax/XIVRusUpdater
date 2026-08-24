using System;
using System.Collections.Generic;
using XIVRusUpdater.Core.Components;
using XIVRusUpdater.Models;
using XIVRusUpdater.Services;

namespace XIVRusUpdater.Utils.States;

public sealed class UpdaterState
{
    public List<DownloadState> Download { get; set; } = [];

    public TranslationManifest? LastRemoteStatus { get; set; }

    public NetworkService.AvailabilityStatus Availability { get; set; }
        = NetworkService.AvailabilityStatus.Disabled;

    public bool PenumbraEnabled { get; set; }

    public ManifestState Translation { get; set; }

    public ManifestState Penumbra { get; set; }
}
