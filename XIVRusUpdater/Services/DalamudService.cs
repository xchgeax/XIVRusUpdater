using Dalamud.Plugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XIVRusUpdater.Services;

public sealed class DalamudService
{
    private readonly object? _dalamudConfig;
    private readonly object? _pluginManager;

    private readonly MethodInfo? _configSaveMethod;
    private readonly MethodInfo? _installPluginMethod;

    private readonly PropertyInfo? _availablePluginsProperty;

    public bool IsAvailable => _dalamudConfig != null && _pluginManager != null;
    
    public DalamudService()
    {
        try
        {
            var assembly = typeof(IDalamudPluginInterface).Assembly;

            var serviceType = assembly.DefinedTypes
                .FirstOrDefault(t => t.Name == "Service`1" && t.IsGenericType);

            var configType = assembly.DefinedTypes
                .FirstOrDefault(t => t.Name == "DalamudConfiguration");

            var pluginManagerType = assembly.DefinedTypes
                .FirstOrDefault(t => t.FullName == "Dalamud.Plugin.Internal.PluginManager");

            if (serviceType == null || configType == null || pluginManagerType == null)
                return;

            var configService = serviceType.MakeGenericType(configType);
            var configGetter = configService.GetMethod("Get", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            _dalamudConfig = configGetter?.Invoke(null, null);

            _configSaveMethod = _dalamudConfig?.GetType().GetMethod("Save", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var pluginManagerService = serviceType.MakeGenericType(pluginManagerType);

            var pluginManagerGetter = pluginManagerService.GetMethod("Get", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            _pluginManager = pluginManagerGetter?.Invoke(null, null);

            _installPluginMethod = pluginManagerType.GetMethod("InstallPluginAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            _availablePluginsProperty = pluginManagerType.GetProperty("AvailablePlugins", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
        catch
        {
            _dalamudConfig = null;
            _pluginManager = null;
        }
    }

    public bool EnsureThirdPartyRepo(string repoUrl)
    {
        try
        {
            if (_dalamudConfig == null)
                return false;

            var repoListProperty = _dalamudConfig.GetType().GetProperty("ThirdRepoList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (repoListProperty?.GetValue(_dalamudConfig) is not IList repoList)
                return false;

            var repoType = repoList.GetType().GetGenericArguments().FirstOrDefault();
            if (repoType == null)
                return false;

            var urlProperty = repoType.GetProperty("Url");
            var enabledProperty = repoType.GetProperty("IsEnabled");

            if (urlProperty == null || enabledProperty == null)
                return false;

            var exists = repoList.Cast<object>()
                .Any(repo =>
                {
                    var url = urlProperty.GetValue(repo) as string;
                    return string.Equals(url, repoUrl, StringComparison.OrdinalIgnoreCase);
                });

            if (exists)
                return true;

            var repoEntry = Activator.CreateInstance(repoType);
            if (repoEntry == null)
                return false;

            urlProperty.SetValue(repoEntry, repoUrl);
            enabledProperty.SetValue(repoEntry, true);

            repoList.Add(repoEntry);

            _configSaveMethod?.Invoke(_dalamudConfig, null);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public object? FindPluginManifest(string internalName)
    {
        try
        {
            if (_pluginManager == null || _availablePluginsProperty == null)
                return null;

            var plugins =
                _availablePluginsProperty.GetValue(_pluginManager) as IEnumerable;

            if (plugins == null)
                return null;

            foreach (var plugin in plugins)
            {
                var name = plugin.GetType().GetProperty("InternalName")?.GetValue(plugin) as string;

                if (string.Equals(name, internalName, StringComparison.OrdinalIgnoreCase))
                {
                    return plugin;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> InstallPluginAsync(string internalName)
    {
        try
        {
            if (_pluginManager == null || _installPluginMethod == null)
                return false;

            var manifest = FindPluginManifest(internalName);
            if (manifest == null)
                return false;

            var loadReasonType = _installPluginMethod.GetParameters()[2].ParameterType;

            var loadReason = Enum.Parse(loadReasonType, "Installer");

            var task = (Task?)_installPluginMethod.Invoke(
                _pluginManager,
                new[]
                {
                    manifest,
                    false,
                    loadReason,
                });

            if (task == null)
                return false;

            await task;

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ReloadRepositoriesAsync(bool notify = false)
    {
        try
        {
            if (_pluginManager == null)
                return false;

            var method = _pluginManager.GetType().GetMethod("SetPluginReposFromConfigAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
                return false;

            var task = method.Invoke(_pluginManager, new object[] { notify }) as Task;

            if (task == null)
                return false;

            await task;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsPluginInstalled(string internalName)
    {
        try
        {
            if (_pluginManager == null)
                return false;

            var installedPluginsProperty = _pluginManager.GetType().GetProperty("InstalledPlugins", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (installedPluginsProperty?.GetValue(_pluginManager) is not IEnumerable plugins)
                return false;

            foreach (var plugin in plugins)
            {
                var manifestProperty = plugin.GetType().GetProperty("Manifest", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                var manifest = manifestProperty?.GetValue(plugin);
                if (manifest == null)
                    continue;

                var internalNameProperty = manifest.GetType().GetProperty("InternalName");

                var name = internalNameProperty?.GetValue(manifest) as string;

                if (string.Equals(name, internalName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> EnsurePluginInstalledAsync(string internalName, string? repoUrl = null)
    {
        try
        {
            if (IsPluginInstalled(internalName))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(repoUrl))
            {
                var added = EnsureThirdPartyRepo(repoUrl);
                
                if(added)
                    await ReloadRepositoriesAsync();
            }

            if (FindPluginManifest(internalName) == null)
                return false;

            return await InstallPluginAsync(internalName);
        }
        catch
        {
            return false;
        }
    }
}
