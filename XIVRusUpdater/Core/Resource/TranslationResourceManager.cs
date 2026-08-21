using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XIVRusUpdater.Core.Resource;

public sealed class TranslationResourceManager : IDisposable
{
    private readonly string _xrtDir;

    private readonly Dictionary<string, XRTFile> _xrtCache = new();
    
    public TranslationResourceManager(string dataDir)
    {
        _xrtDir = Path.Combine(dataDir, "xrt");

        if (!Directory.Exists(_xrtDir))
            return;

        foreach (var file in Directory.EnumerateFiles(_xrtDir, "*.xrt", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(_xrtDir, file);

            relative = Path.ChangeExtension(relative, null)!;

            var sheetName = relative.Replace(Path.DirectorySeparatorChar, '/');

            _xrtCache[sheetName] = new XRTFile(file);
        }
    }

    private static string ToPath(string baseDir, string sheetName, string ext)
    {
        var relative = sheetName.Replace('/', Path.DirectorySeparatorChar) + ext;
        return Path.Combine(baseDir, relative);
    }

    public bool TryGetXRT(string sheetName, out XRTFile data)
    {
        if (_xrtCache.TryGetValue(sheetName, out data)) return true;
        var path = ToPath(_xrtDir, sheetName, ".xrt");
        if (!File.Exists(path)) return false;
        _xrtCache[sheetName] = data = new XRTFile(path);
        return true;
    }

    public bool HasSheet(string sheetName) => File.Exists(ToPath(_xrtDir, sheetName, ".xrt"));

    public List<string> GetAllSheets() => _xrtCache.Keys.ToList();

    public void UnloadAll()
    {
        foreach(var (_, file) in _xrtCache)
            file.Dispose();
        _xrtCache.Clear();
    }

    public void Dispose() => UnloadAll();
}
