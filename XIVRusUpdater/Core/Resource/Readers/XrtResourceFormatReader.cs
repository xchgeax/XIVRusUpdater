using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Core.Resource.Readers;

internal sealed class XrtResourceFormatReader : IResourceFormatReader
{
    public Dictionary<uint, List<ByteArrayWrapper>> Read(Stream stream)
    {
        using var reader = new BinaryReader(stream);
        var rows = new Dictionary<uint, List<ByteArrayWrapper>>();

        // TODO: Реализовать формат .xrt

        return rows;
    }
}
