using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace XIVRusUpdater;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    #region UI

    public bool ShowNotifications { get; set; } = true;

    public bool ShowChangelogAfterUpdate { get; set; } = true;

    #endregion

    #region Updates

    public int UpdateCheckIntervalMinutes { get; set; } = 60;

    public bool AutoDownloadUpdates { get; set; } = true;

    public bool AutoInstallUpdates { get; set; } = true;

    #endregion

    #region Tester Access

    public bool TesterHumanCheck { get; set; }

    public UpdateChannel Channel { get; set; } = UpdateChannel.Stable;

    #endregion

    #region State

    public string LastInstalledVersion { get; set; } = string.Empty;

    public string LastKnownRemoteVersion { get; set; } = string.Empty;

    public DateTime LastUpdateCheck { get; set; }

    public DateTime LastSuccessfulUpdate { get; set; }

    #endregion

    #region Components
    public Dictionary<string, bool> EnabledComponents { get; set; } = new()
    {
        ["enemy_npc"] = true,
        ["place_names"] = true,
        ["content_finder"] = true,
        ["npc_names"] = true,
        ["actions"] = true,
        ["achievements"] = true,
        ["titles"] = true,
        ["collectibles"] = true,
        ["emotes"] = true,
    };

    #endregion
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}

public enum UpdateChannel
{
    Stable,
    Beta
}
