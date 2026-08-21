using Dalamud.Game.Text.SeStringHandling;
using Lumina.Text.ReadOnly;
using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;
using XIVRusUpdater.Core.Resource;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Core;

public class TranslationParser : IDisposable
{
    private TranslationResourceManager _ResourceManager { get; set; }

    public TranslationParser()
    {
        _ResourceManager = new TranslationResourceManager(Plugin.PluginInterface.AssemblyLocation.Directory.FullName);
    }

    public bool TryGetValue(string sheetName, uint RowId, uint Column, out ByteArrayWrapper? translation)
    {
        translation = null;
        ByteArrayWrapper result;
        if (_ResourceManager.TryGetXRT(sheetName, out var xrtFile))
        {
            if (xrtFile.TryGetData(RowId, Column, out var @byte))
            {
                translation = @byte;
                return true;
            }
        }
        return false;
    }

    public void Dispose()
    {
        _ResourceManager.Dispose();
    }
}
