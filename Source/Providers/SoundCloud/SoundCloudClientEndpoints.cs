using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AngleSharp;
using AngleSharp.Dom;
using JsonExtensions.Http;

namespace YtPlaylist.SoundCloud;

partial class SoundCloudClient
{
    public async Task<User?> GetUserFromPermalink(string permalink, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri($"/{permalink}", UriKind.Relative));
        request.Headers.Clear();
        request.Headers.Add("Cookie", GetCookies());
        request.Headers.Add("Host", "soundcloud.com");
        request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
        request.Headers.Add("Upgrade-Insecure-Requests", "0");
        request.Headers.Add("Sec-Fetch-Dest", "document");
        request.Headers.Add("Sec-Fetch-Mode", "navigate");
        request.Headers.Add("Sec-Fetch-Site", "none");
        request.Headers.Add("Sec-Fetch-User", "?1");
        request.Headers.Add("Priority", "u=0, i");
        request.Headers.Add("Pragma", "no-cache");
        request.Headers.Add("Cache-Control", "no-cache");

        HttpResponseMessage res = await SendRequest(request, true, cancellationToken);
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        res.EnsureSuccessStatusCode();
        HandleResponse(res);

        string body = await res.Content.ReadAsStringAsync(cancellationToken);
        IConfiguration config = Configuration.Default;
        using IBrowsingContext context = BrowsingContext.New(config);
        using IDocument document = await context.OpenAsync(req => req.Content(body), cancel: cancellationToken);

        HtmlPageMeta pageMeta = ParseHtmlPage(document);

        if (pageMeta.Hydrations.TryGetValue("user", out JsonElement _user) && _user.ValueKind == JsonValueKind.Object)
        {
            return _user.Deserialize<User>() ?? throw new JsonException();
        }

        return null;
    }

    public async Task<IReadOnlyList<WebProfile>> GetUserWebProfiles(long userId, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri($"/users/soundcloud:users:{userId}/web-profiles?{BuildQueryParameters()}", UriKind.Relative));
        request.Headers.Clear();
        PrepareXHRRequest(request, false);

        HttpResponseMessage res = await SendRequest(request, true, cancellationToken);
        res.EnsureSuccessStatusCode();
        HandleResponse(res);

        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken: cancellationToken);
        return body.Deserialize<IReadOnlyList<WebProfile>>() ?? throw new JsonException();
    }

    public async Task<PlaylistsResponse> GetPlaylistsWithoutAlbums(long userId, int limit = 10, int offset = 0, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri($"/users/{userId}/playlists_without_albums?{BuildQueryParameters(
            ("limit", limit),
            ("offset", offset),
            ("linked_partitioning", 1)
        )}", UriKind.Relative));
        request.Headers.Clear();
        PrepareXHRRequest(request, false);

        HttpResponseMessage res = await SendRequest(request, false, cancellationToken);
        res.EnsureSuccessStatusCode();
        HandleResponse(res);

        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken: cancellationToken);
        return body.Deserialize<PlaylistsResponse>() ?? throw new JsonException();
    }

    public async Task<SearchResponse> Search(string query, int limit = 20, int offset = 0, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri($"/search?{BuildQueryParameters(
            ("q", query),
            ("facet", "model"),
            ("limit", limit),
            ("offset", offset),
            ("linked_partitioning", 1)
        )}", UriKind.Relative));
        request.Headers.Clear();
        PrepareXHRRequest(request, false);

        HttpResponseMessage res = await SendRequest(request, true, cancellationToken);
        res.EnsureSuccessStatusCode();
        HandleResponse(res);

        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken: cancellationToken);
        return body.Deserialize<SearchResponse>() ?? throw new JsonException();
    }

    public async Task<TrackSearchResponse> SearchTracks(SearchRequestTrackFilter query, CancellationToken cancellationToken = default)
    {
        List<(string Key, object Value)> parameters = [
            ("q", query.Query),
            ("facet", "genre"),
            ("limit", query.Limit),
            ("offset", query.Offset),
            ("linked_partitioning", 1)
        ];

        if (query.GenreOrTag is not null) parameters.Add(("filter.genre_or_tag", query.GenreOrTag));
        if (query.Genre is not null) parameters.Add(("filter.genre", query.Genre));
        if (query.Duration is not DurationFilter.Any)
        {
            parameters.Add(("filter.duration", query.Duration switch
            {
                DurationFilter.Short => "short",
                DurationFilter.Medium => "medium",
                DurationFilter.Long => "long",
                DurationFilter.Epic => "epic",
                _ or DurationFilter.Any => throw new UnreachableException(),
            }));
        }

        if (query.CreatedAt is not CreatedAtFilter.Any)
        {
            parameters.Add(("filter.created_at", query.CreatedAt switch
            {
                CreatedAtFilter.LastHour => "last_hour",
                CreatedAtFilter.LastDay => "last_day",
                CreatedAtFilter.LastWeek => "last_week",
                CreatedAtFilter.LastMonth => "last_month",
                CreatedAtFilter.LastYear => "last_year",
                _ or CreatedAtFilter.Any => throw new UnreachableException(),
            }));
        }

        if (query.License is not LicenseFilter.Any)
        {
            parameters.Add(("filter.license", query.License switch
            {
                LicenseFilter.ToModifyCommercially => "to_modify_commercially",
                LicenseFilter.ToShare => "to_share",
                LicenseFilter.ToUseCommercially => "to_use_commercially",
                _ or LicenseFilter.Any => throw new UnreachableException(),
            }));
        }

        using HttpRequestMessage request = new(HttpMethod.Get, new Uri($"/search/tracks?{BuildQueryParameters(parameters)}", UriKind.Relative));
        request.Headers.Clear();
        PrepareXHRRequest(request, false);

        HttpResponseMessage res = await SendRequest(request, true, cancellationToken);
        res.EnsureSuccessStatusCode();
        HandleResponse(res);

        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken: cancellationToken);
        return body.Deserialize<TrackSearchResponse>() ?? throw new JsonException();
    }

    public async Task<CreatePlaylistResponse> CreatePlaylist(UpdatePlaylistContent playlist, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"/playlists?{BuildQueryParameters()}", UriKind.Relative));
        PrepareXHRRequest(request, true);

        request.Content = JsonContent.Create(new
        {
            playlist = new
            {
                title = playlist.Title,
                sharing = playlist.Sharing,
                tracks = playlist.Tracks,
                artwork_url = playlist.ArtworkUrl,
                description = playlist.Description,
                genre = playlist.Genre,
                release_date = playlist.ReleaseDate,
                tag_list = playlist.TagList,
            }
        });

        HttpResponseMessage res = await SendRequest(request, false, cancellationToken);
        res.EnsureSuccessStatusCode();
        HandleResponse(res);

        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken: cancellationToken);
        return body.Deserialize<CreatePlaylistResponse>() ?? throw new JsonException();
    }

    public async IAsyncEnumerable<Playlist> GetPlaylists(long userId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int offset = 0;
        while (true)
        {
            PlaylistsResponse res = await GetPlaylists(userId, 10, offset, cancellationToken);
            offset += res.Collection.Count;
            if (res.Collection.Count == 0) break;
            foreach (Playlist item in res.Collection)
            {
                yield return item;
            }
        }
    }

    public async Task<PlaylistsResponse> GetPlaylists(long userId, int limit, int offset, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri($"/users/{userId}/playlists?{BuildQueryParameters(
            ("limit", limit),
            ("offset", offset),
            ("linked_partitioning", 1)
        )}", UriKind.Relative));
        request.Headers.Clear();
        PrepareXHRRequest(request, true);

        HttpResponseMessage res = await SendRequest(request, false, cancellationToken);
        res.EnsureSuccessStatusCode();
        HandleResponse(res);

        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken: cancellationToken);
        return body.Deserialize<PlaylistsResponse>() ?? throw new JsonException();
    }

    public async Task DeletePlaylist(long playlistId, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Delete, new Uri($"/playlists/{playlistId}?{BuildQueryParameters()}", UriKind.Relative));
        request.Headers.Clear();
        PrepareXHRRequest(request, true);

        HttpResponseMessage res = await SendRequest(request, false, cancellationToken);
        res.EnsureSuccessStatusCode();
        HandleResponse(res);
    }

    public async Task<Me> GetMe(CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri($"/me?{BuildQueryParameters()}", UriKind.Relative));
        request.Headers.Clear();
        PrepareXHRRequest(request, true);

        HttpResponseMessage res = await SendRequest(request, false, cancellationToken);
        res.EnsureSuccessStatusCode();
        HandleResponse(res);

        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken: cancellationToken);
        return body.Deserialize<Me>() ?? throw new JsonException();
    }

    public async Task<UpdatePlaylistResponse> UpdatePlaylistItems(long playlistId, IEnumerable<long> tracks, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Put, new Uri($"/playlists/{playlistId}?{BuildQueryParameters()}", UriKind.Relative));
        request.Headers.Clear();
        PrepareXHRRequest(request, true);

        request.Content = JsonContent.Create(new
        {
            playlist = new
            {
                tracks = tracks,
            }
        });

        HttpResponseMessage res = await SendRequest(request, false, cancellationToken);
        res.EnsureSuccessStatusCode();
        HandleResponse(res);

        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken);
        return body.Deserialize<UpdatePlaylistResponse>() ?? throw new JsonException();
    }

    public async Task<UpdatePlaylistResponse> UpdatePlaylist(long playlistId, UpdatePlaylistContent playlist, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Put, new Uri($"/playlists/{playlistId}?{BuildQueryParameters()}", UriKind.Relative));
        request.Headers.Clear();
        PrepareXHRRequest(request, true);

        request.Content = JsonContent.Create(new UpdatePlaylistRequest()
        {
            Playlist = playlist
        });

        HttpResponseMessage res = await SendRequest(request, false, cancellationToken);
        res.EnsureSuccessStatusCode();
        HandleResponse(res);

        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken);
        return body.Deserialize<UpdatePlaylistResponse>() ?? throw new JsonException();
    }

    public async Task<Playlist> GetPlaylist(long playlistId, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri($"/playlists/{playlistId}?{BuildQueryParameters()}", UriKind.Relative));
        request.Headers.Clear();
        PrepareXHRRequest(request, true);

        HttpResponseMessage res = await SendRequest(request, false, cancellationToken);
        res.EnsureSuccessStatusCode();
        HandleResponse(res);

        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken);
        return body.Deserialize<Playlist>() ?? throw new JsonException();
    }

    public async Task<Track> GetTracks(IEnumerable<long> tracks, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri($"/tracks?{BuildQueryParameters(
            ("ids", string.Join(',', tracks))
        )}", UriKind.Relative));
        request.Headers.Clear();
        PrepareXHRRequest(request, true);

        HttpResponseMessage res = await SendRequest(request, true, cancellationToken);
        res.EnsureSuccessStatusCode();
        HandleResponse(res);

        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken);
        return body.Deserialize<Track>() ?? throw new JsonException();
    }
}
