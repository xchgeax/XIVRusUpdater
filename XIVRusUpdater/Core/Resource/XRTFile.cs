using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Core.Resource;

public class XRTFile : IDisposable
{
    public Dictionary<uint, List<ByteArrayWrapper>> Rows { get; init;  } 

    public XRTFile(string filePath)
    {
        if (!Path.Exists(filePath))
            throw new FileNotFoundException();

        using (BinaryReader reader = new BinaryReader(File.OpenRead(filePath)))
        {
            // TODO: Implement Reading after base design created
        }
    }

    public bool TryGetData(uint rowId, uint column, out ByteArrayWrapper? value)
    {
        value = null;

        if (!Rows.TryGetValue(rowId, out var row))
            return false;

        if (column >= row.Count)
            return false;

        value = row[(int)column];
        return true;
    }

    public void Dispose()
    {
        foreach(var (RowId, Columns) in Rows)
        {
            foreach(var col in Columns)
            {
                col.Dispose();
            }
        }

        Rows.Clear();
    }
}
