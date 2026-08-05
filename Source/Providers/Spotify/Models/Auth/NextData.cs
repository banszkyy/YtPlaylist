using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData
{
    [J("props")] public NextData_Props? Props { get; init; }
    [J("page")] public string? Page { get; init; }
    [J("query")] public NextData_Query? Query { get; init; }
    [J("buildId")] public string? BuildId { get; init; }
    [J("assetPrefix")] public string? AssetPrefix { get; init; }
    [J("runtimeConfig")] public NextData_RuntimeConfig? RuntimeConfig { get; init; }
    [J("isFallback")] public bool? IsFallback { get; init; }
    [J("isExperimentalCompile")] public bool? IsExperimentalCompile { get; init; }
    [J("gssp")] public bool? Gssp { get; init; }
    [J("appGip")] public bool? AppGip { get; init; }
    [J("locale")] public string? Locale { get; init; }
    [J("locales")] public IReadOnlyList<string>? Locales { get; init; }
    [J("defaultLocale")] public string? DefaultLocale { get; init; }
    [J("scriptLoader")] public IReadOnlyList<object>? ScriptLoader { get; init; }
}
