using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData_Props
{
    [J("pageProps")] public NextData_PageProps? PageProps { get; init; }
    [J("initialToken")] public string? InitialToken { get; init; }
    [J("isCsrfEnabled")] public bool? IsCsrfEnabled { get; init; }
    [J("origin")] public string? Origin { get; init; }
    [J("trackingEnabled")] public bool? TrackingEnabled { get; init; }
    [J("translationSet")] public IReadOnlyDictionary<string, string>? TranslationSet { get; init; }
    [J("__N_SSP")] public bool? NSsp { get; init; }
}
