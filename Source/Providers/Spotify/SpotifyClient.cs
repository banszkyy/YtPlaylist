using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using System.Web;
using AngleSharp;
using AngleSharp.Dom;
using HttpCache;
using JsonExtensions.Http;
using Logger;

namespace YtPlaylist.Spotify;

sealed partial class SpotifyClient : IDisposable
{
    readonly HttpClient client;
    readonly IRequestCache? cache;
    DateTimeOffset lastRequestTime;
    const int CooldownMilliseconds = 1500;
    ExchangeDeviceCodeResponse? token;
    DateTimeOffset tokenExpiresAt;
    readonly Dictionary<string, string> cookies = [];
    GrantedClientToken? clientToken;
    DateTimeOffset clientTokenExpiresAt;
    DateTimeOffset clientTokenRefreshAt;
    readonly AppArguments arguments;
    readonly string clientId;
    readonly string appVersion;
    readonly string deviceFlowUserAgent;
    readonly string userAgent;

    public SpotifyClient(SpotifyCredentials credentials, ImmutableArray<NetscapeCookieFile.Cookie> cookies, AppArguments arguments, IRequestCache? cache)
    {
        this.userAgent = credentials.UserAgent ?? "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36";
        this.arguments = arguments;
        this.cache = cache;
        clientId = "65b708073fc0480ea92a077233ca87bd";
        appVersion = "1.2.96.301.g6a125c73";
        deviceFlowUserAgent = "Spotify/128700414 Win32_x86_64/Windows 10 (10.0.26100; x64)";

        clientToken = credentials.ClientToken is null ? null : new GrantedClientToken()
        {
            Token = credentials.ClientToken,
            Domains = [],
            ExpiresAfterSeconds = 1000000,
            RefreshAfterSeconds = 1000000,
        };

        client = new(new HttpClientHandler()
        {
            AllowAutoRedirect = false,
        })
        {
            BaseAddress = new Uri("https://api-partner.spotify.com"),
            DefaultRequestVersion = new Version(2, 0),
        };
        client.DefaultRequestHeaders.Clear();

        foreach (NetscapeCookieFile.Cookie cookie in cookies)
        {
            if (!cookie.Domain.EndsWith("spotify.com")) continue;
            this.cookies.Add(cookie.Name, cookie.Value);
        }
    }

    void PrepareXHRRequest(HttpRequestMessage request)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
        request.Headers.Add("Origin", "https://open.spotify.com");
        request.Headers.Add("Sec-GPC", "1");
        request.Headers.Add("Sec-Fetch-Dest", "empty");
        request.Headers.Add("Sec-Fetch-Mode", "cors");
        request.Headers.Add("Sec-Fetch-Site", "same-site");
        request.Headers.Add("app-platform", "WebPlayer");
        request.Headers.Add("spotify-app-version", appVersion);
        request.Headers.Add("user-agent", userAgent);
        if (clientToken is not null) request.Headers.Add("client-token", clientToken.Token);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        request.Headers.Referrer = new Uri("https://open.spotify.com/", UriKind.Absolute);
        request.Headers.Add("Cookie", string.Join("; ", cookies.Where(v => v.Key == "sp_key").Select(v => $"{v.Key}={v.Value}")));
    }

    async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, bool cached, Dictionary<string, string>? additionalCacheKey = null, CancellationToken cancellationToken = default)
    {
        Debug.Assert(request.RequestUri is not null);

        string cacheKey = additionalCacheKey is null ? request.RequestUri.ToString() : $"{request.RequestUri}{(string.IsNullOrEmpty(request.RequestUri.Query) ? "?" : "&") + string.Join('&', additionalCacheKey.OrderBy(v => v.Key).Select(v => $"{HttpUtility.UrlEncode(v.Key)}={HttpUtility.UrlEncode(v.Value)}"))}";

        if (cached && cache is not null && cache.TryGetCachedItem(cacheKey, out Stream? stream, out HttpStatusCode status))
        {
            return new HttpResponseMessage(status)
            {
                RequestMessage = request,
                Content = new StreamContent(stream!),
            };
        }

        if (lastRequestTime != default)
        {
            double ellapsedMilliseconds = (DateTimeOffset.UtcNow - lastRequestTime).TotalMilliseconds;
            if (ellapsedMilliseconds < CooldownMilliseconds)
            {
                await Task.Delay(CooldownMilliseconds - (int)Math.Max(0, ellapsedMilliseconds), cancellationToken);
            }
        }

        HttpResponseMessage res = await client.SendAsync(request, cancellationToken);
        lastRequestTime = DateTimeOffset.UtcNow;

        if (cache is not null)
        {
            Stream content = await res.Content.ReadAsStreamAsync(cancellationToken);
            cache.Add(cacheKey, content, res.StatusCode);

            HttpResponseMessage res2 = new(res.StatusCode)
            {
                ReasonPhrase = res.ReasonPhrase,
                Version = res.Version,
                RequestMessage = res.RequestMessage,
                Content = new StreamContent(content),
            };
            foreach (KeyValuePair<string, IEnumerable<string>> item in res.Headers)
            {
                res2.Headers.Add(item.Key, item.Value);
            }
            foreach (KeyValuePair<string, IEnumerable<string>> item in res.TrailingHeaders)
            {
                res2.TrailingHeaders.Add(item.Key, item.Value);
            }
            return res2;
        }

        return res;
    }

    async Task<JsonElement> PathfinderRequest(PathfinderRequest request, bool cached, Dictionary<string, string>? additionalCacheKey = null, CancellationToken cancellationToken = default)
    {
        await AuthorizeIfRequired(cancellationToken);

        using HttpRequestMessage _request = new(HttpMethod.Post, new Uri($"https://api-partner.spotify.com/pathfinder/v2/query", UriKind.Absolute));
        _request.Version = new Version(2, 0);
        _request.Headers.Clear();
        PrepareXHRRequest(_request);
        _request.Content = JsonContent.Create(request);

        additionalCacheKey ??= [];
        additionalCacheKey.Add("_", request.OperationName);

        HttpResponseMessage res = await SendRequest(_request, cached, additionalCacheKey, cancellationToken);
        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken: cancellationToken);
        if (res.StatusCode == HttpStatusCode.BadRequest)
        {
            if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("errors", out JsonElement errorsE))
            {
                int l = errorsE.GetArrayLength();
                List<string> errors = [];
                for (int i = 0; i < l; i++)
                {
                    errors.Add(errorsE[i].GetProperty("message").GetString() ?? "?");
                }
                throw new SpotifyException(string.Join('\n', errors));
            }
        }
        res.EnsureSuccessStatusCode();

        return body;
    }

    async Task<T> PathfinderRequest<T>(PathfinderRequest request, bool cached, Dictionary<string, string>? additionalCacheKey = null, CancellationToken cancellationToken = default)
    {
        JsonElement body = await PathfinderRequest(request, cached, additionalCacheKey, cancellationToken);
        return body.Deserialize<T>() ?? throw new JsonException();
    }

    void HandleResponse(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookies))
        {
            foreach (string setCookie in setCookies)
            {
                string[] v = setCookie.Split(';', StringSplitOptions.TrimEntries);
                cookies[v[0].Split('=')[0]] = v[0].Split('=')[1];
            }
        }
    }

    async Task<ExchangeDeviceCodeResponse> GetToken(CancellationToken cancellationToken = default)
    {
        if (token is not null)
        {
            Log.MinorAction($"Refreshing token");

            using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"https://accounts.spotify.com/api/token", UriKind.Absolute));
            request.Version = new Version(2, 0);
            request.Headers.Clear();
            request.Headers.Add("Cookie", string.Join("; ", cookies.Select(v => $"{v.Key}={v.Value}")));
            request.Headers.Add("User-Agent", deviceFlowUserAgent);

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "client_id", clientId },
                { "refresh_token", token.RefreshToken },
            });

            HttpResponseMessage res = await client.SendAsync(request, cancellationToken);
            HandleResponse(res);
            JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken);
            res.EnsureSuccessStatusCode();
            lastRequestTime = DateTimeOffset.UtcNow;

            return body.Deserialize<ExchangeDeviceCodeResponse>() ?? throw new JsonException();
        }
        else
        {
            Log.MinorAction($"Authorizing");

            DeviceAuthorizationResponse auth_data = await InitiateDeviceAuthorization(cancellationToken);
            string device_code = auth_data.DeviceCode;
            string user_code = auth_data.UserCode;
            string verification_url = auth_data.VerificationUriComplete;
            (string? flow_ctx, string? csrf_token) = await ParseVerificationPage(new Uri(verification_url, UriKind.Absolute), cancellationToken);
            await SubmitUserCode(user_code, flow_ctx, csrf_token, verification_url, cancellationToken);
            ExchangeDeviceCodeResponse token_data = await ExchangeDeviceCode(device_code, cancellationToken);
            return token_data;
        }
    }

    async Task<DeviceAuthorizationResponse> InitiateDeviceAuthorization(CancellationToken cancellationToken = default)
    {
        Log.Debug($"Initiating device authorization");
        using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"https://accounts.spotify.com/oauth2/device/authorize", UriKind.Absolute));
        request.Version = new Version(2, 0);
        request.Headers.Clear();
        request.Headers.Add("Cookie", string.Join("; ", cookies.Select(v => $"{v.Key}={v.Value}")));
        request.Headers.Add("User-Agent", deviceFlowUserAgent);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id",  clientId },
            { "scope",  "app-remote-control,playlist-modify,playlist-modify-private,playlist-modify-public,playlist-read,playlist-read-collaborative,playlist-read-private,streaming,transfer-auth-session,ugc-image-upload,user-follow-modify,user-follow-read,user-library-modify,user-library-read,user-modify,user-modify-playback-state,user-modify-private,user-personalized,user-read-birthdate,user-read-currently-playing,user-read-email,user-read-play-history,user-read-playback-position,user-read-playback-state,user-read-private,user-read-recently-played,user-top-read" },
        });

        HttpResponseMessage res = await client.SendAsync(request, cancellationToken);
        lastRequestTime = DateTimeOffset.UtcNow;
        HandleResponse(res);
        res.EnsureSuccessStatusCode();

        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken);
        return body.Deserialize<DeviceAuthorizationResponse>() ?? throw new JsonException();
    }

    static Dictionary<string, string> ParseQuery(Uri uri)
    {
        Dictionary<string, string> res = [];
        string v = uri.Query;
        if (v.StartsWith('?')) v = v[1..];

        foreach (string item in v.Split('&'))
        {
            int i = item.IndexOf('=');
            if (i == -1 || i + 1 >= item.Length) res.Add(item, string.Empty);
            else res.Add(item[..i], item[(i + 1)..]);
        }

        return res;
    }

    async Task<(string flow_ctx, string csrf_token)> ParseVerificationPage(Uri verification_url, CancellationToken cancellationToken = default)
    {
        Log.Debug($"Fetching verification page");

        HttpResponseMessage res;
        while (true)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, verification_url);
            request.Version = new Version(2, 0);
            request.Headers.Clear();
            request.Headers.Add("User-Agent", userAgent);
            request.Headers.Add("Sec-Fetch-Dest", "document");
            request.Headers.Add("Sec-Fetch-Mode", "navigate");
            request.Headers.Add("Sec-Fetch-Site", "none");
            request.Headers.Add("Sec-Fetch-User", "?1");
            request.Headers.Add("Sec-GPC", "1");
            request.Headers.Add("Cookie", string.Join("; ", cookies.Select(v => $"{v.Key}={v.Value}")));

            res = await client.SendAsync(request, cancellationToken);
            lastRequestTime = DateTimeOffset.UtcNow;
            HandleResponse(res);

            if (res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently or HttpStatusCode.TemporaryRedirect)
            {
                Uri redirectTo = res.Headers.Location ?? throw new UnreachableException();
                verification_url = new Uri(verification_url, redirectTo);
                res.Dispose();
                continue;
            }

            break;
        }

        res.EnsureSuccessStatusCode();

        string flow_ctx_full = HttpUtility.UrlDecode(ParseQuery(res.RequestMessage!.RequestUri!)["flow_ctx"]);

        string html = await res.Content.ReadAsStringAsync(cancellationToken);

        IConfiguration config = Configuration.Default;
        using IBrowsingContext context = BrowsingContext.New(config);
        using IDocument document = await context.OpenAsync(req => req.Content(html), cancel: cancellationToken);

        IElement? nextDataE = document.QuerySelector("#__NEXT_DATA__");
        IElement? useravatarNameE = document.QuerySelector("span[data-testid=useravatar-name]");

        if (nextDataE is null) throw new SpotifyException($"Verification page doesn't have next data");
        if (useravatarNameE is null) throw new SpotifyException($"Verification page is invalid");

        NextData? nextData = JsonSerializer.Deserialize<NextData>(nextDataE.InnerHtml);
        string csrf_token = nextData?.Props?.InitialToken ?? throw new SpotifyException($"Couldn't extract csrf token from page");

        return (flow_ctx_full, csrf_token);
    }

    async Task SubmitUserCode(
        string user_code,
        string flow_ctx,
        string csrf_token,
        string referer_url,
        CancellationToken cancellationToken = default)
    {
        Log.Debug($"Waiting 4s");
        await Task.Delay(4000, cancellationToken);

        Log.Debug($"Submitting user code");

        long current_ts = long.Parse(flow_ctx.Split(':')[1]) + 4;

        using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"https://accounts.spotify.com/pair/api/resolve?flow_ctx={flow_ctx.Split(':')[0]}:{current_ts}", UriKind.Absolute));
        request.Version = new Version(2, 0);
        request.Headers.Clear();
        request.Headers.Add("X-CSRF-Token", csrf_token);
        request.Headers.Add("Referer", referer_url);
        request.Headers.Add("Origin", "https://accounts.spotify.com");
        request.Headers.Add("Cookie", string.Join("; ", cookies.Select(v => $"{v.Key}={v.Value}")));
        request.Headers.Add("User-Agent", deviceFlowUserAgent);

        request.Content = JsonContent.Create(new
        {
            code = user_code
        });

        HttpResponseMessage res = await client.SendAsync(request, cancellationToken);
        lastRequestTime = DateTimeOffset.UtcNow;
        HandleResponse(res);
        JsonElement body;
        if (res.Content.Headers.ContentType?.MediaType == MediaTypeNames.Application.Json)
        {
            body = await res.Content.ReadAsJsonAsync(cancellationToken);
        }
        res.EnsureSuccessStatusCode();


    }

    async Task<ExchangeDeviceCodeResponse> ExchangeDeviceCode(string device_code, CancellationToken cancellationToken = default)
    {
        Log.Debug($"Exchanging device code");

        using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"https://accounts.spotify.com/api/token", UriKind.Absolute));
        request.Version = new Version(2, 0);
        request.Headers.Clear();
        request.Headers.Add("Cookie", string.Join("; ", cookies.Select(v => $"{v.Key}={v.Value}")));
        request.Headers.Add("User-Agent", deviceFlowUserAgent);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" },
            { "client_id", clientId },
            { "device_code", device_code },
        });

        HttpResponseMessage res = await client.SendAsync(request, cancellationToken);
        lastRequestTime = DateTimeOffset.UtcNow;
        HandleResponse(res);
        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken);
        res.EnsureSuccessStatusCode();

        return body.Deserialize<ExchangeDeviceCodeResponse>() ?? throw new JsonException();
    }

    async Task<ClientTokenResponse> FetchClientToken(string deviceId, CancellationToken cancellationToken = default)
    {
        Log.MinorAction($"Fetching client token");

        using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"https://clienttoken.spotify.com/v1/clienttoken", UriKind.Absolute));
        request.Version = new Version(2, 0);
        request.Headers.Clear();
        request.Headers.Add("User-Agent", userAgent);
        request.Headers.Add("Referer", "https://open.spotify.com/");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
        request.Headers.Add("Sec-GPC", "1");
        request.Headers.Add("Sec-Fetch-Dest", "empty");
        request.Headers.Add("Sec-Fetch-Mode", "cors");
        request.Headers.Add("Sec-Fetch-Site", "same-site");
        request.Headers.Add("Priority", "u=4");

        request.Content = JsonContent.Create(new
        {
            client_data = new
            {
                client_id = clientId,
                client_version = appVersion,
                js_sdk_data = new
                {
                    device_brand = "unknown",
                    device_id = deviceId,
                    device_model = "unknown",
                    device_type = "computer",
                    os = userAgent.Contains("linux", StringComparison.InvariantCultureIgnoreCase) ? "linux" : "windows",
                    os_version = "unknown",
                },
            },
        });

        HttpResponseMessage res = await client.SendAsync(request, cancellationToken);
        lastRequestTime = DateTimeOffset.UtcNow;
        HandleResponse(res);
        res.EnsureSuccessStatusCode();
        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken);

        return body.Deserialize<ClientTokenResponse>() ?? throw new JsonException();
    }

    async Task AuthorizeIfRequired(CancellationToken cancellationToken = default)
    {
        string spotifyTokenFile = Path.Combine(arguments.HttpCachePath, "spotify-token.json");
        string spotifyClientTokenFile = Path.Combine(arguments.HttpCachePath, "spotify-client-token.json");

        if (token is null || tokenExpiresAt <= DateTimeOffset.UtcNow)
        {
            token = await GetToken(cancellationToken);
            tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);

            Directory.CreateDirectory(Path.GetDirectoryName(spotifyTokenFile)!);
            File.WriteAllText(spotifyTokenFile, JsonSerializer.Serialize(new SavedSpotifyToken()
            {
                Token = token,
                ExpiresAt = tokenExpiresAt,
            }));
        }

        //if (clientToken is null || clientTokenRefreshAt <= DateTimeOffset.UtcNow || clientTokenExpiresAt <= DateTimeOffset.UtcNow)
        //{
        //    if (!cookies.TryGetValue("sp_t", out string? deviceId))
        //    {
        //        throw new SpotifyException($"Cookie \"sp_t\" doesn't exists");
        //    }
        //
        //    ClientTokenResponse v = await FetchClientToken(deviceId, cancellationToken);
        //    clientToken = v.GrantedToken;
        //    clientTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(v.GrantedToken.ExpiresAfterSeconds);
        //    clientTokenRefreshAt = DateTimeOffset.UtcNow.AddSeconds(v.GrantedToken.RefreshAfterSeconds);
        //
        //    Directory.CreateDirectory(Path.GetDirectoryName(spotifyClientTokenFile)!);
        //    File.WriteAllText(spotifyClientTokenFile, JsonSerializer.Serialize(new SavedClientToken()
        //    {
        //        Token = clientToken,
        //        ExpiresAt = clientTokenExpiresAt,
        //        RefreshAt = clientTokenRefreshAt,
        //    }));
        //}
    }

    public async Task Initialize(CancellationToken cancellationToken = default)
    {
        string spotifyTokenFile = Path.Combine(arguments.HttpCachePath, "spotify-token.json");
        string spotifyClientTokenFile = Path.Combine(arguments.HttpCachePath, "spotify-client-token.json");

        if (File.Exists(spotifyTokenFile))
        {
            SavedSpotifyToken v = JsonSerializer.Deserialize<SavedSpotifyToken>(File.ReadAllText(spotifyTokenFile)) ?? throw new JsonException(); ;
            token = v.Token;
            tokenExpiresAt = v.ExpiresAt;
        }

        if (File.Exists(spotifyClientTokenFile))
        {
            SavedClientToken v = JsonSerializer.Deserialize<SavedClientToken>(File.ReadAllText(spotifyClientTokenFile)) ?? throw new JsonException(); ;
            clientToken = v.Token;
            clientTokenExpiresAt = v.ExpiresAt;
            clientTokenRefreshAt = v.RefreshAt;
        }

        await AuthorizeIfRequired(cancellationToken);
    }

    public void Dispose() => client.Dispose();
}
