using nietras.SeparatedValues;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Core.Resource.Readers;

//Relative easy task, no overload: https://www.joelverhagen.com/blog/2020/12/fastest-net-csv-parsers
//Fastest lib - Sep. 
internal sealed class CsvResourceFormatReader : IResourceFormatReader
{
    public Dictionary<uint, List<ByteArrayWrapper>> Read(Stream stream)
    {
        var rows = new Dictionary<uint, List<ByteArrayWrapper>>();

        using var reader = Sep.Reader(o => o with { HasHeader = false }).From(stream);

        foreach (var row in reader)
        {
            var rowId = row[0].Parse<uint>();

            var columns = new List<ByteArrayWrapper>(row.ColCount - 1);

            for (var i = 1; i < row.ColCount; i++)
            {
                var span = row[i].Span;
                var byteCount = Encoding.UTF8.GetByteCount(span);
                var bytes = new byte[byteCount];
                Encoding.UTF8.GetBytes(span, bytes);
                columns.Add(new ByteArrayWrapper(bytes));
            }

            rows[rowId] = columns;
        }

        return rows;
    }
}
