using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData_Props
{
    [J("pageProps")] public NextData_PageProps? PageProps { get; set; }
    [J("initialToken")] public string? InitialToken { get; set; }
    [J("isCsrfEnabled")] public bool? IsCsrfEnabled { get; set; }
    [J("origin")] public string? Origin { get; set; }
    [J("trackingEnabled")] public bool? TrackingEnabled { get; set; }
    [J("translationSet")] public Dictionary<string, string>? TranslationSet { get; set; }
    [J("__N_SSP")] public bool? NSsp { get; set; }
}
