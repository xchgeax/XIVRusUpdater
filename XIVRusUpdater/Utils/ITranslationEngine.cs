using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XIVRusUpdater;

namespace XIVRusUpdater.Utils;

public class ITranslationEngine
{
    public string modName { get; private set; }
    public string API_BASE { get; private set; }

    public DirectoryInfo? ModPath
    {
        get
        {
            return Plugin.PenumbraApi.GetModPath(modName);
        }
    }

    public string Version
    {
        get
        {
            return Plugin.PenumbraApi.GetModVersion(modName) ?? "0.0.0";
        }
    }

    public ITranslationEngine(string modName, string api)
    {
        this.modName = modName;
        API_BASE = api;
    }
}
