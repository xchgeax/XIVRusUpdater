using CheapLoc;

namespace XIVRusUpdater.Utils;

public static class Translations
{
    #region MainWindow

    public static string MainWindowTitle => Loc.Localize("Title.MainWindow", "XIV Rus Auto Updater");

    #region System Status
    public static string SystemStatusHeader => Loc.Localize("Headers.System", "System Status");
    public static string PenumbraStatus => Loc.Localize("Status.Penumbra", "Penumbra: {0}");
    public static string XIVRusStatus => Loc.Localize("Status.XIVRus", "XIV Rus: {0}");
    public static string VersionStatus => Loc.Localize("Status.Version", "Version: {0}");
    public static string ServerStatus => Loc.Localize("Status.Server", "Server Status: {0}");
    #endregion

    #region Version Information
    public static string VersionHeader => Loc.Localize("Headers.Version", "Version Information");
    public static string GameVersion => Loc.Localize("Information.GameVersion", "Game version: {0}");
    public static string InstalledVersion => Loc.Localize("Information.InstalledVersion", "Installed: {0}");
    public static string RemoteVersion => Loc.Localize("Information.RemoteVersion", "Latest: {0}");
    public static string LastCheck => Loc.Localize("Information.LastCheck", "Last Check:");
    #endregion

    #region Changelog
    public static string ChangelogHeader => Loc.Localize("Headers.Changelog", "Last Changelog");
    public static string NoChangelog => Loc.Localize("Changelog.None", "No changelog available.");
    #endregion

    #region Actions
    public static string ActionsHeader => Loc.Localize("Headers.Actions", "Actions");
    public static string RefreshButton => Loc.Localize("Actions.Refresh", "Refresh");
    public static string UpdateButton => Loc.Localize("Actions.Update", "Update XIV Rus");
    public static string ReloadButton => Loc.Localize("Actions.Reload", "Reload game");
    public static string OpenConfigButton => Loc.Localize("Actions.OpenConfig", "Open config");
    #endregion

    #region Diagnostics
    public static string DiagnosticsHeader => Loc.Localize("Headers.Diagnostics", "Diagnostics");
    public static string Branch => Loc.Localize("Diagnostics.Branch", "Branch:");
    public static string TesterAllowance => Loc.Localize("Diagnostics.TesterAllowance", "Tester Access Allowance");
    public static string TesterAllowed => Loc.Localize("Diagnostics.TesterAllowed", "Allowed");
    public static string TesterDenied => Loc.Localize("Diagnostics.TesterDenied", "Not Allowed");
    #endregion

    #region Overall Status
    public static string StatusUpToDate => Loc.Localize("Status.UpToDate", "XIV Rus is up to date");
    public static string StatusUpdateAvailable => Loc.Localize("Status.UpdateAvailable", "Update available");
    public static string StatusDisabled => Loc.Localize("Status.Disabled", "XIV Rus temporarily disabled");
    public static string StatusError => Loc.Localize("Status.Error", "Unable to determine status");
    #endregion

    #endregion

    #region DownloadWindow

    public static string DownloadTitle => Loc.Localize("Download.Title", "Downloading XIV Rus");
    public static string DownloadProgress => Loc.Localize("Download.Progress", "{0} MB / {1} MB");
    public static string DownloadSource => Loc.Localize("Download.Source", "Current Source:");
    public static string DownloadSpeed => Loc.Localize("Download.Speed", "Speed: {0} MB/s");

    #endregion

    #region ConfigWindow

    public static string ConfigWindowTitle => Loc.Localize("Title.ConfigWindow", "XIV Rus Config");

    #region General
    public static string GeneralHeader => Loc.Localize("Headers.General", "General");
    public static string ShowNotifications => Loc.Localize("Config.ShowNotifications", "Show notifications");
    public static string ShowChangelogAfterUpdate => Loc.Localize("Config.ShowChangelog", "Show changelog after update");
    #endregion

    #region Updates
    public static string UpdatesHeader => Loc.Localize("Headers.Updates", "Updates");
    public static string CheckInterval => Loc.Localize("Updates.Interval", "Check interval (minutes)");
    public static string AutoDownloadUpdates => Loc.Localize("Updates.AutoDownload", "Auto download updates");
    public static string AutoInstallUpdates => Loc.Localize("Updates.AutoInstall", "Auto install updates");
    #endregion

    #region Tester Access
    public static string TesterHeader => Loc.Localize("Headers.Tester", "Tester Access");
    public static string TesterChannel => Loc.Localize("Tester.Channel", "Channel");
    public static string TesterWarning => Loc.Localize(
        "Tester.Warning",
        "Test versions may contain unverified translations, incomplete changes, and unexpected issues. The game or localization may behave incorrectly.");
    public static string TesterAgreement => Loc.Localize(
        "Tester.Agreement",
        "I understand the risks of using test versions.");
    #endregion

    #region Information
    public static string InformationHeader => Loc.Localize("Headers.Information", "Information");
    public static string InstalledVersionLabel => Loc.Localize("Information.InstalledVersionLabel", "Installed Version:");
    public static string LatestVersionLabel => Loc.Localize("Information.LatestVersionLabel", "Latest Version:");
    public static string LastUpdateCheckLabel => Loc.Localize("Information.LastUpdateCheckLabel", "Last Update Check:");
    public static string LastSuccessfulUpdateLabel => Loc.Localize("Information.LastSuccessfulUpdateLabel", "Last Successful Update:");
    #endregion

    #endregion

    #region ChangelogWindow

    public static string ChangelogWindowTitle => Loc.Localize("Title.ChangelogWindow", "XIV Rus Update Changelog");
    public static string ChangelogUpdated => Loc.Localize("Changelog.Updated", "XIV Rus has been updated.");
    public static string ChangelogUnavailable => Loc.Localize("Changelog.Unavailable", "No changelog available.");
    public static string AcceptButton => Loc.Localize("Buttons.Accept", "Accept");
    public static string AcceptAndRestartButton => Loc.Localize("Buttons.AcceptAndRestart", "Accept and Restart");

    #region Restart Confirmation
    public static string RestartConfirmTitle => Loc.Localize("RestartConfirm.Title", "Restart Confirm");

    public static string RestartWarning => Loc.Localize(
        "RestartConfirm.Warning",
        "The game client will be reloaded.\n\nMake sure you are not in combat, a duty, a cutscene, or performing any activity that could be interrupted.");

    public static string RestartQuestion => Loc.Localize(
        "RestartConfirm.Question",
        "Do you understand the consequences and wish to continue?");

    public static string CancelButton => Loc.Localize("Buttons.Cancel", "Cancel");
    public static string UnderstandButton => Loc.Localize("Buttons.Understand", "I Understand");
    #endregion

    #endregion

    #region Confirmation

    public static string ConfirmationQuestion => Loc.Localize("Confirmation.Question", "Are you sure?");
    public static string ConfirmationConfirm => Loc.Localize("Confirmation.Confirm", "Confirm");
    public static string ConfirmationCancel => Loc.Localize("Confirmation.Cancel", "Cancel");

    #endregion
}
