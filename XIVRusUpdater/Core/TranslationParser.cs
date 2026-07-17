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
    // (string SheetName, uint RowId) => Translation Row
    public Dictionary<(string, uint), TranslationRow> Sheets { get; }
    private TranslationResourceManager _ResourceManager { get; set; }

    public bool TryGetValue(string sheetName, uint RowId, int Column, out ByteArrayWrapper? translation)
    {
        if(Sheets.TryGetValue((sheetName, RowId), out var translationRow))
        {
            if(translationRow.Columns.TryGetValue(Column, out ByteArrayWrapper? result))
            {
                if (result != null)
                {
                    translation = result;
                    return true;
                }
            }
        }
        translation = null;
        return false;
    }

    public TranslationParser()
    {
        _ResourceManager = new(Plugin.PluginInterface.AssemblyLocation.Directory.FullName);

        foreach (var sheet in _ResourceManager.GetAllSheets())
        {
            if(_ResourceManager.TryGetXRT(sheet, out var file))
            {
                foreach(var row in file.rows)
                {
                    TranslationRow translation = new TranslationRow();
                    for(int i = 0; i < row.TextFields.Count; ++i)
                    {
                        var columnTrans = row.TextFields[i];
                        translation.Add(i, columnTrans);
                    }
                    TryAdd(sheet, row.RowId, translation);
                }    
            }
        }
    }

    public bool TryAdd(string sheetName, uint RowID, TranslationRow row) => Sheets.TryAdd((sheetName, RowID), row);

    public void Dispose()
    {
        foreach (var row in Sheets.Values) row.Dispose();
    }

    public sealed class TranslationRow : IDisposable
    {
        public readonly Dictionary<int, ByteArrayWrapper> Columns = new Dictionary<int, ByteArrayWrapper>();

        public bool Add(int column, byte[] @byte) => Columns.TryAdd(column, new ByteArrayWrapper(@byte));

        public bool Add(int column, ByteArrayWrapper @byte) => Columns.TryAdd(column, @byte);

        public int Count() => Columns.Count;

        public void Dispose()
        {
            foreach(var row in Columns.Values) row.Dispose();
        }
    }
}
