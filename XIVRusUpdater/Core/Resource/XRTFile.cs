using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XIVRusUpdater.Core.Resource;

public class XRTFile
{
    public XRTFile(string filePath)
    {
        if (!Path.Exists(filePath))
            throw new FileNotFoundException();

        using (BinaryReader reader = new BinaryReader(File.OpenRead(filePath)))
        {
            // TODO: Implement Reading after base design created
        }
    }
}
