using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using XIVRusUpdater.Core.Components;

namespace XIVRusUpdater.Core;

public sealed class TranslationFilter
{
    private volatile HashSet<string> _activeSheets = new();
    private volatile List<string> _activePrefixes = new();
    private volatile List<string> _allPrefixes = new();
    private volatile HashSet<string> _allModified = new();

    public void Rebuild(IReadOnlySet<string> disabledComponents)
    {
        var activeSheets = new HashSet<string>();
        var activePrefixes = new List<string>();
        var allPrefixes = new List<string>();
        var allModified = new HashSet<string>();

        foreach (var component in TranslationComponents.All)
        {
            foreach (var sheet in component.Sheets)
                allModified.Add(sheet);

            if (component.IsWildcard)
                allPrefixes.Add(component.WildcardPrefix);

            if (disabledComponents.Contains(component.Id))
                continue;

            if (component.IsWildcard)
            {
                activePrefixes.Add(component.WildcardPrefix);
            }
            else
            {
                activeSheets.UnionWith(component.Sheets);
            }
        }

        _activeSheets = activeSheets;
        _activePrefixes = activePrefixes;
        _allPrefixes = allPrefixes;
        _allModified = allModified;
    }

    public bool IsActive(string sheetName)
    {
        foreach (var prefix in _activePrefixes)
        {
            if (sheetName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var prefix in _allPrefixes)
        {
            if (sheetName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;
        }
            
        if (_activeSheets.Contains(sheetName))
            return true;

        if (_allModified.Contains(sheetName))
            return false;

        return true;
    }
}
