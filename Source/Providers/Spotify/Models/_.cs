using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public partial class AppServerConfig
{
    [J("appName")] public string? AppName { get; set; }
    [J("market")] public string? Market { get; set; }
    [J("locale")] public Locale? Locale { get; set; }
    [J("userCountry")] public string? UserCountry { get; set; }
    [J("isPremium")] public bool? IsPremium { get; set; }
    [J("correlationId")] public string? CorrelationId { get; set; }
    [J("isAnonymous")] public bool? IsAnonymous { get; set; }
    [J("gtmId")] public string? GtmId { get; set; }
    [J("pipResources")] public PipResources? PipResources { get; set; }
    [J("retargetingPixels")] public object? RetargetingPixels { get; set; }
    [J("recaptchaWebPlayerFraudSiteKey")] public string? RecaptchaWebPlayerFraudSiteKey { get; set; }
    [J("serverTime")] public long? ServerTime { get; set; }
    [J("clientVersion")] public string? ClientVersion { get; set; }
    [J("buildVersion")] public string? BuildVersion { get; set; }
    [J("buildDate")] public DateTimeOffset? BuildDate { get; set; }
}

public partial class Locale
{
    [J("locale")] public string? LocaleLocale { get; set; }
    [J("rtl")] public bool? Rtl { get; set; }
    [J("textDirection")] public string? TextDirection { get; set; }
}

public partial class PipResources
{
    [J("scripts")] public List<Uri>? Scripts { get; set; }
    [J("styles")] public List<Uri>? Styles { get; set; }
}

public class ServerTimeResponse
{
    [J("serverTime")] public required long ServerTime { get; set; }
}
