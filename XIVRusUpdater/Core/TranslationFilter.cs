using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace XIVRusUpdater.Core;

public sealed class TranslationFilter
{
    private volatile HashSet<string> _activeSheets = new();
    private volatile List<string> _activePrefixes = new();
    private volatile List<string> _allModified = new();

    public void Rebuild(Dictionary<string, bool> enabledComponents)
    {
        var sheets = new HashSet<string>();
        var prefixes = new List<string>();
        var all = new List<string>();

        foreach (var def in ComponentDefinitions.All)
        {
            foreach (var sheet in def.Sheets)
                all.Add(sheet);
            if (!enabledComponents.GetValueOrDefault(def.Id)) continue;

            if (def.IsWildcard)
                prefixes.Add(def.WildcardPrefix);
            else
                foreach (var sheet in def.Sheets)
                    sheets.Add(sheet);
        }

        _activeSheets = sheets;
        _activePrefixes = prefixes;
        _allModified = all;
    }

    public bool IsActive(string sheetName)
    {
        if (_activeSheets.Contains(sheetName) && !_allModified.Contains(sheetName)) return true;
        var prefixes = _activePrefixes;
        for (int i = 0; i < prefixes.Count; i++)
            if (sheetName.StartsWith(prefixes[i], StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
