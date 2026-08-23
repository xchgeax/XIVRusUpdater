using System;
using System.Collections.Generic;
using System.IO;
using XIVRusUpdater.Core.Resource.Readers;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Core.Resource;

public class FileResource : IDisposable
{
    // RowId -> String Columns Allocation
    public Dictionary<uint, List<ByteArrayWrapper>> Rows { get; init;  }

    private static readonly Dictionary<ResourceFormat, Func<IResourceFormatReader>> Readers = new()
    {
        [ResourceFormat.Xrt] = () => new XrtResourceFormatReader(),
        [ResourceFormat.Csv] = () => new CsvResourceFormatReader(),
    };

    public FileResource(string filePath, ResourceFormat format)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException(null, filePath);

        if (!Readers.TryGetValue(format, out var factory))
            throw new NotSupportedException($"Format '{format}' is not supported.");

        using var stream = File.OpenRead(filePath);
        Rows = factory().Read(stream);
    }

    public FileResource(string filePath, string format)
        : this(filePath, ResourceFormatParser.Parse(format))
    {
    }

    public FileResource(string filePath)
        : this(filePath, ResourceFormatParser.FromExtension(filePath))
    {
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
