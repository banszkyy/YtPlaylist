using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using HttpCache;

namespace YtPlaylist;

sealed class LrcLib(IRequestCache cache, HarLogger? har) : IDisposable
{
    readonly MeowHttpClient Client = HttpClientFactory.CreateMeow("LrcLib", cache, har, c =>
    {
        c.DefaultRequestHeaders.Add("User-Agent", App.UserAgent);
        c.BaseAddress = new Uri("https://lrclib.net/");
    });

    public sealed class LyricsResponse
    {
        [JsonPropertyName("id")] public required int Id { get; set; }
        [JsonPropertyName("trackName")] public required string TrackName { get; set; }
        [JsonPropertyName("artistName")] public required string ArtistName { get; set; }
        [JsonPropertyName("albumName")] public required string AlbumName { get; set; }
        [JsonPropertyName("duration")] public required double Duration { get; set; }
        [JsonPropertyName("instrumental")] public required bool Instrumental { get; set; }
        [JsonPropertyName("plainLyrics")] public required string? PlainLyrics { get; set; }
        [JsonPropertyName("syncedLyrics")] public required string? SyncedLyrics { get; set; }
    }

    public async Task<LyricsResponse?> FetchLyrics(string artistName, string trackName, string? albumName, int? duration, CancellationToken cancellationToken = default)
    {
        string uri;

        {
            StringBuilder uriBuilder = new();
            uriBuilder.Append("/api/get");

            uriBuilder.Append($"?artist_name={HttpUtility.UrlEncode(artistName)}");
            uriBuilder.Append($"&track_name={HttpUtility.UrlEncode(trackName)}");
            if (albumName is not null) uriBuilder.Append($"&album_name={HttpUtility.UrlEncode(albumName)}");
            if (duration.HasValue) uriBuilder.Append($"&duration={duration.Value.ToString()}");
            uri = uriBuilder.ToString();
        }

        using HttpResponseMessage response = await Client.GetAsync(uri, cached: true, cancellationToken: cancellationToken);
        Stream? stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        if ((int)response.StatusCode is not (>= 200 and <= 299))
        {
            throw new HttpRequestException(
                null,
                null,
                response.StatusCode
            );
        }

        using StreamReader reader = new(stream);
        string text = await reader.ReadToEndAsync(cancellationToken);
        return JsonSerializer.Deserialize<LyricsResponse>(text);
    }

    public void Dispose()
    {
        Client.Dispose();
    }
}