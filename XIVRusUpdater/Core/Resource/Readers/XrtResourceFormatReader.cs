using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XIVRusUpdater.Utils;
using XIVRusUpdater.Utils.Extentions;

namespace XIVRusUpdater.Core.Resource.Readers;

internal sealed class XrtResourceFormatReader : IResourceFormatReader
{
    private static readonly byte[] Magic = "XRT\0"u8.ToArray();

    public Dictionary<uint, List<ByteArrayWrapper>> Read(Stream stream)
    {
        using var reader = new BinaryReader(stream);

        var magic = reader.ReadBytes(Magic.Length);

        if (!magic.AsSpan().SequenceEqual(Magic))
            throw new InvalidDataException("Invalid XRT format.");

        ushort version = reader.ReadUInt16();

        if (version != 1)
            throw new InvalidDataException($"Unsupported XRT version: {version}");

        ushort stringColumnCount = reader.ReadUInt16();

        var rows = new Dictionary<uint, List<ByteArrayWrapper>>();


        while (stream.Position < stream.Length)
        {
            long remain = stream.Length - stream.Position;

            if (remain < sizeof(uint))
                throw new InvalidDataException("Incomplete XRT row header.");

            uint rowId = reader.ReadUInt32();

            var columns = new List<ByteArrayWrapper>(stringColumnCount);

            for (int i = 0; i < stringColumnCount; i++)
            {
                byte[] value = reader.ReadStringNullterminated();
                columns.Add(new ByteArrayWrapper(value));
            }

            rows.Add(rowId, columns);
        }

        return rows;
    }
}
