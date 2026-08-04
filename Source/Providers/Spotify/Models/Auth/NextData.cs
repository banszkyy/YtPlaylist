using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData
{
    [J("props")] public NextData_Props? Props { get; set; }
    [J("page")] public string? Page { get; set; }
    [J("query")] public NextData_Query? Query { get; set; }
    [J("buildId")] public string? BuildId { get; set; }
    [J("assetPrefix")] public string? AssetPrefix { get; set; }
    [J("runtimeConfig")] public NextData_RuntimeConfig? RuntimeConfig { get; set; }
    [J("isFallback")] public bool? IsFallback { get; set; }
    [J("isExperimentalCompile")] public bool? IsExperimentalCompile { get; set; }
    [J("gssp")] public bool? Gssp { get; set; }
    [J("appGip")] public bool? AppGip { get; set; }
    [J("locale")] public string? Locale { get; set; }
    [J("locales")] public List<string>? Locales { get; set; }
    [J("defaultLocale")] public string? DefaultLocale { get; set; }
    [J("scriptLoader")] public List<object>? ScriptLoader { get; set; }
}
