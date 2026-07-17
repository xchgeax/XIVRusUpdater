using Dalamud.Plugin;
using Dalamud.Plugin.Ipc.Exceptions;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using Penumbra.Api.IpcSubscribers;
using Penumbra.Api.IpcSubscribers.Legacy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XIVRusUpdater.Services;

public sealed class PenumbraService
{
    private Penumbra.Api.IpcSubscribers.GetModList GetListMod { get; } = null!;
    private Penumbra.Api.IpcSubscribers.InstallMod InstallMod { get; } = null!;
    private Penumbra.Api.IpcSubscribers.DeleteMod DeleteMod { get; } = null!;
    private Penumbra.Api.IpcSubscribers.ReloadMod ReloadMod { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetEnabledState GetEnableStatus { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetModListAdapter ModList { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetCollection GetCollection { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetCurrentModSettings ModSettings { get; } = null!;
    private Penumbra.Api.IpcSubscribers.GetModDirectory GetDirectory { get; } = null!;
    
    private const string InternalName = "Penumbra";
    
    public PenumbraService(IDalamudPluginInterface @interface)
    {
        GetListMod = new (@interface);
        InstallMod = new (@interface);
        DeleteMod = new (@interface);
        ReloadMod = new (@interface);
        GetEnableStatus = new (@interface);
        ModList = new (@interface);
        GetCollection = new (@interface);
        ModSettings = new (@interface);
        GetDirectory = new (@interface);
    }

    public bool IsInstalled()
    {
        return Plugin.PluginInterface.InstalledPlugins.Any(p => p.InternalName.Equals(InternalName, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsPenumbraEnabled() => GetEnableStatus.Invoke();
    
    private Guid? GetDefaultCollection()
    {
        return GetCollection.Invoke(Penumbra.Api.Enums.ApiCollectionType.Default)?.Id;
    }

    public string GetDefaultDirectory() => GetDirectory.Invoke();

    public bool IsModInstalled(string modName) => GetListMod.Invoke().ContainsKey(modName);
    
    public string? GetModVersion(string modName)
    {
        if (!IsModInstalled(modName)) return null;

        var modList = ModList.Invoke();
        var mod = modList.FirstOrDefault(x => x.Identifier == modName);
        var modVersion = mod.Version;
        return modVersion;
    }

    public DirectoryInfo? GetModPath(string modName)
    {
        if (!IsModInstalled(modName)) return null;

        var modList = ModList.Invoke();
        var mod = modList.FirstOrDefault(x => x.Identifier == modName);
        return mod.ModPath;
    }

    public bool DeleteMods(string modName)
    {
        var responce = DeleteMod.Invoke(string.Empty, modName);

        if (responce == Penumbra.Api.Enums.PenumbraApiEc.Success) return true;

        Plugin.Log.Error($"Failed to delete plugin: Recieve {Enum.GetName(responce)} from Penumbra");
        return false;
    }

    public bool ReloadMods(string modName)
    {
        var response = ReloadMod.Invoke(string.Empty, modName);

        if (response == Penumbra.Api.Enums.PenumbraApiEc.Success) return true;

        Plugin.Log.Error($"Failed to reload plugin: Recieve {Enum.GetName(response)} from Penumbra");
        return false;
    }

    public bool InstallMods(string downloadPath)
    {
        var response = InstallMod.Invoke(downloadPath);

        if (response == Penumbra.Api.Enums.PenumbraApiEc.Success) return true;

        Plugin.Log.Error($"Failed to install plugin: Recieve {Enum.GetName(response)} from Penumbra");
        return false;
    }
}
