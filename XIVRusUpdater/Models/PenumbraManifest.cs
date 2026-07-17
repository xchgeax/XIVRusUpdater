using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace XIVRusUpdater.Models;

public class PenumbraManifest
{
    [JsonProperty("version")]
    public string? RusVersion { get; set; }

    [JsonProperty("changelog")]
    public string? Changelog { get; set; }

    [JsonProperty("urls")]
    public List<string> Urls { get; set; } = new List<string>();
}
