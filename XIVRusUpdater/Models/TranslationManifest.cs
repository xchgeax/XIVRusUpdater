using Newtonsoft.Json;
using System.Collections.Generic;

namespace XIVRusUpdater.Models;

public sealed class TranslationManifest
{
    [JsonProperty("version")] 
    public string Version { get; init; } = "";

    [JsonProperty("changelog")] 
    public string Changelog { get; init; } = "";

    [JsonProperty("downloadUrls")] 
    public List<string> DownloadUrl { get; init; } = [];
}
