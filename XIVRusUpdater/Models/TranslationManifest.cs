using Newtonsoft.Json;
using System.Collections.Generic;

namespace XIVRusUpdater.Models;

public sealed class TranslationManifest
{
    [JsonProperty("version")] 
    public string Version { get; init; } = string.Empty;
    public string PenumbraVersion { get; init; } = string.Empty;

    [JsonProperty("changelog")] 
    public string Changelog { get; init; } = string.Empty;
    public string PenumbraChangelog {  get; init; } = string.Empty;

    [JsonProperty("downloadUrls")] 
    public List<string> DownloadUrl { get; init; } = [];
    public List<string> PenumbraDownloadUrls { get; init; } = [];
}
