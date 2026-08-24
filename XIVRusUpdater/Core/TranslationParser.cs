using Dalamud.Plugin;
using System;
using XIVRusUpdater.Core.Components;
using XIVRusUpdater.Core.Resource;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Core;

public class TranslationParser : IDisposable
{
    private TranslationResourceManager _ResourceManager { get; set; }
    private string _CurrentEngineId { get; set; }

    public TranslationParser(string @engineId)
    {
        _CurrentEngineId = engineId;
        _ResourceManager = CreateResourceManager(_CurrentEngineId);
    }

    public void UpdateEngine(string engineId)
    {
        if (engineId == _CurrentEngineId) return;

        var newManager = CreateResourceManager(engineId);

        _ResourceManager.Dispose();
        _ResourceManager = newManager;
        _CurrentEngineId = engineId;
    }

    private static TranslationResourceManager CreateResourceManager(string engineId)
    {
        var engine = TranslationEngines.Get(engineId)
            ?? throw new ArgumentException($"Unknown translation engine: {engineId}", nameof(engineId));

        return new TranslationResourceManager(Plugin.PluginInterface.AssemblyLocation.Directory!.FullName, engine.Format);
    }

    public bool TryGetValue(string sheetName, uint RowId, uint Column, out ByteArrayWrapper? translation)
    {
        translation = null;
        if (_ResourceManager.TryGet(sheetName, out var xrtFile))
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
