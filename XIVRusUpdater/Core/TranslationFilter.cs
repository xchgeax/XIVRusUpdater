using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using XIVRusUpdater.Core.Components;

namespace XIVRusUpdater.Core;

sealed class TranslationFilter
{
    private volatile HashSet<string> _activeSheets = new();
    private volatile List<string> _activePrefixes = new();

    private volatile HashSet<string> _allSheets = new();
    private volatile List<string> _allPrefixes = new();

    public void Rebuild(IReadOnlySet<string> disabledComponents)
    {
        var activeSheets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var activePrefixes = new List<string>();

        var allSheets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allPrefixes = new List<string>();

        foreach (var component in TranslationComponents.All)
        {
            bool disabled = disabledComponents.Contains(component.Id);

            foreach (var sheet in component.Sheets.Keys)
            {
                if (sheet.EndsWith('*'))
                {
                    var prefix = sheet[..^1];

                    allPrefixes.Add(prefix);

                    if (!disabled)
                        activePrefixes.Add(prefix);
                }
                else
                {
                    allSheets.Add(sheet);

                    if (!disabled)
                        activeSheets.Add(sheet);
                }
            }
        }

        _activeSheets = activeSheets;
        _activePrefixes = activePrefixes;
        _allSheets = allSheets;
        _allPrefixes = allPrefixes;
    }

    public bool IsActive(string sheetName)
    {
        if (_activeSheets.Contains(sheetName))
            return true;

        if (_allSheets.Contains(sheetName))
            return false;

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

        return true;
    }
}
