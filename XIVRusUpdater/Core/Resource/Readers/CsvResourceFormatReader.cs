using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using nietras.SeparatedValues;
using System;
using System.Collections.Generic;
using System.IO;
using XIVRusUpdater.Core.Resource.Readers;
using XIVRusUpdater.Utils;

internal sealed class CsvResourceFormatReader : IResourceFormatReader
{
    private const string UnsupportedMacroError = "ERROR: Unsupported MacroCode";

    public Dictionary<uint, List<ByteArrayWrapper?>> Read(Stream stream)
    {
        var rows = new Dictionary<uint, List<ByteArrayWrapper?>>();

        using var reader = Sep.Reader(o => o with { HasHeader = false })
            .From(stream);

        List<int>? stringColumnIndices = null;

        foreach (var row in reader)
        {
            if (row.ColCount == 0)
            {
                continue;
            }

            var firstCol = row[0].Span;

            if (IsServiceRow(firstCol))
            {
                continue;
            }

            if (firstCol.SequenceEqual("Int32"))
            {
                stringColumnIndices = new List<int>();

                for (var i = 0; i < row.ColCount; i++)
                {
                    if (row[i].Span.SequenceEqual("String"))
                    {
                        stringColumnIndices.Add(i);
                    }
                }

                continue;
            }

            if (stringColumnIndices is null ||
                !int.TryParse(firstCol, out var parsedRowId) ||
                parsedRowId < 0)
            {
                continue;
            }

            // columns[i] всегда соответствует i-й строковой колонке из заголовка CSV, даже если значение отсутствует или не удалось распарсить.
            var columns = new List<ByteArrayWrapper?>(stringColumnIndices.Count);

            foreach (var colIdx in stringColumnIndices)
            {
                if (colIdx >= row.ColCount)
                {
                    columns.Add(null);
                    continue;
                }

                var textSpan = row[colIdx].Span.Trim('"');

                if (TryParseMacroString(textSpan, out var bytes))
                {
                    columns.Add(new ByteArrayWrapper(bytes));
                }
                else
                {
                    columns.Add(null);
                }
            }

            if (columns.Count > 0)
            {
                rows[(uint)parsedRowId] = columns;
            }
        }

        return rows;
    }

    private static bool IsServiceRow(ReadOnlySpan<char> firstColumn) =>
        firstColumn.SequenceEqual("key") ||
        firstColumn.SequenceEqual("#") ||
        firstColumn.SequenceEqual("offset");

    private static bool TryParseMacroString(ReadOnlySpan<char> input, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();

        try
        {
            var str = input.ToString();
            var builder = new SeStringBuilder();
            builder.AppendMacroString(str);

            var seString = builder.Build();

            var textValue = seString.TextValue;
            if (textValue?.Contains(UnsupportedMacroError, StringComparison.Ordinal) == true)
            {
                return false;
            }

            bytes = seString.EncodeWithNullTerminator();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
