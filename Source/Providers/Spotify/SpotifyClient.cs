using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
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
    readonly MeowHttpClient client;
    string? accessToken;
    string? refreshToken;
    DateTimeOffset tokenExpiresAt;
    string? clientToken;
    DateTimeOffset clientTokenExpiresAt;
    DateTimeOffset clientTokenRefreshAt;
    readonly AppArguments arguments;
    string? clientId;
    string? appVersion;
    readonly string deviceId;
    readonly string deviceFlowUserAgent;
    readonly string userAgent;
    ClientWebSocket? ws;

    public SpotifyClient(SpotifyCredentials credentials, ImmutableArray<NetscapeCookieFile.Cookie> cookies, AppArguments arguments, IRequestCache? cache, HarLogger? har)
    {
        this.userAgent = credentials.UserAgent ?? "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36";
        this.arguments = arguments;
        appVersion = "1.2.87.27.ga2033a72";
        deviceFlowUserAgent = "Spotify/128700414 Win32_x86_64/Windows 10 (10.0.26100; x64)";

        deviceId = cookies.FirstOrDefault(v => v.Name == "sp_t").Value ?? throw new SpotifyException($"Cookie \"sp_t\" doesn't exists");

        client = HttpClientFactory.CreateMeow("Spotify", cache, har, c =>
        {
            //c.BaseAddress = new Uri("https://api-partner.spotify.com");
            c.DefaultRequestVersion = new Version(2, 0);
            c.DefaultRequestHeaders.Clear();
        }, c =>
        {
            CookieContainer cookieContainer = new();
            foreach (NetscapeCookieFile.Cookie cookie in cookies) cookie.AddTo(cookieContainer);
            c.CookieContainer = cookieContainer;
            c.UseCookies = true;
        });
    }

    void PrepareXHRRequest(HttpRequestMessage request)
    {
        request.Version = new Version(2, 0);
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
        if (clientToken is not null) request.Headers.Add("client-token", clientToken);
        if (accessToken is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Referrer = new Uri("https://open.spotify.com/", UriKind.Absolute);
    }

    async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, bool cached, Dictionary<string, string>? additionalCacheKey = null, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync(request, CacheUtils.GetKey(request.RequestUri!, additionalCacheKey), cached, cancellationToken);
    }

    async Task<JsonElement> PathfinderRequest(PathfinderRequest request, bool cached, Dictionary<string, string>? additionalCacheKey = null, CancellationToken cancellationToken = default)
    {
        await AuthorizeIfRequired(cancellationToken);

        using HttpRequestMessage _request = new(HttpMethod.Post, new Uri($"https://api-partner.spotify.com/pathfinder/v2/query", UriKind.Absolute));
        _request.Version = new Version(2, 0);
        _request.Headers.Clear();
        PrepareXHRRequest(_request);
        _request.Content = JsonContent.Create(request, new MediaTypeHeaderValue(MediaTypeNames.Application.Json, null));

        additionalCacheKey ??= [];
        additionalCacheKey.Add("_", request.OperationName);

        using HttpResponseMessage res = await SendRequest(_request, cached, additionalCacheKey, cancellationToken);
        string text = await res.Content.ReadAsStringAsync(cancellationToken);
        JsonElement body = JsonElement.Parse(text);
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

    async Task<ExchangeDeviceCodeResponse> GetToken(CancellationToken cancellationToken = default)
    {
        if (clientId is null) throw new InvalidOperationException($"{nameof(clientId)} is not set");

        if (refreshToken is not null)
        {
            Log.MinorAction($"Refreshing token");

            using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"https://accounts.spotify.com/api/token", UriKind.Absolute));
            request.Version = new Version(2, 0);
            request.Headers.Clear();
            request.Headers.Add("User-Agent", deviceFlowUserAgent);

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "client_id", clientId },
                { "refresh_token", refreshToken },
            });

            HttpResponseMessage res = await client.SendAsync(request, cancellationToken: cancellationToken);
            JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken);
            res.EnsureSuccessStatusCode();

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

        if (clientId is null) throw new InvalidOperationException($"{nameof(clientId)} is not set");

        using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"https://accounts.spotify.com/oauth2/device/authorize", UriKind.Absolute));
        request.Version = new Version(2, 0);
        request.Headers.Clear();
        request.Headers.Add("User-Agent", deviceFlowUserAgent);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id",  clientId },
            { "scope",  "app-remote-control,playlist-modify,playlist-modify-private,playlist-modify-public,playlist-read,playlist-read-collaborative,playlist-read-private,streaming,transfer-auth-session,ugc-image-upload,user-follow-modify,user-follow-read,user-library-modify,user-library-read,user-modify,user-modify-playback-state,user-modify-private,user-personalized,user-read-birthdate,user-read-currently-playing,user-read-email,user-read-play-history,user-read-playback-position,user-read-playback-state,user-read-private,user-read-recently-played,user-top-read" },
        });

        HttpResponseMessage res = await client.SendAsync(request, cancellationToken: cancellationToken);
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

            res = await client.SendAsync(request, cancellationToken: cancellationToken);

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
        request.Headers.Add("User-Agent", deviceFlowUserAgent);

        request.Content = JsonContent.Create(new
        {
            code = user_code
        }, new MediaTypeHeaderValue(MediaTypeNames.Application.Json, null));

        HttpResponseMessage res = await client.SendAsync(request, cancellationToken: cancellationToken);
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

        if (clientId is null) throw new InvalidOperationException($"{nameof(clientId)} is not set");

        using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"https://accounts.spotify.com/api/token", UriKind.Absolute));
        request.Version = new Version(2, 0);
        request.Headers.Clear();
        request.Headers.Add("User-Agent", deviceFlowUserAgent);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" },
            { "client_id", clientId },
            { "device_code", device_code },
        });

        HttpResponseMessage res = await client.SendAsync(request, cancellationToken: cancellationToken);
        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken);
        res.EnsureSuccessStatusCode();

        return body.Deserialize<ExchangeDeviceCodeResponse>() ?? throw new JsonException();
    }

    async Task FetchPage(CancellationToken cancellationToken)
    {
        Log.MinorAction($"Fetching page");

        using HttpRequestMessage request = new(HttpMethod.Get, new Uri($"https://open.spotify.com/", UriKind.Absolute));
        request.Version = new Version(2, 0);
        request.Headers.Clear();
        request.Headers.Add("User-Agent", userAgent);
        request.Headers.Add("Referer", "https://open.spotify.com/");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Text.Html));
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
        request.Headers.Add("Sec-GPC", "1");
        request.Headers.Add("Sec-Fetch-Dest", "document");
        request.Headers.Add("Sec-Fetch-Mode", "navigate");
        request.Headers.Add("Sec-Fetch-Site", "none");
        request.Headers.Add("Sec-Fetch-User", "?1");
        request.Headers.Add("Priority", "u=0, i");

        HttpResponseMessage res = await client.SendAsync(request, cancellationToken: cancellationToken);
        res.EnsureSuccessStatusCode();
        string html = await res.Content.ReadAsStringAsync(cancellationToken);

        using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        using IDocument document = await context.OpenAsync(req => req.Content(html), cancel: cancellationToken);

        IElement appServerConfigE = document.QuerySelector("#appServerConfig") ?? throw new UnreachableException($"Element not found");
        AppServerConfig appServerConfig = JsonSerializer.Deserialize<AppServerConfig>(Base64.DecodeFromChars(appServerConfigE.TextContent)) ?? throw new JsonException();

        appVersion = appServerConfig.ClientVersion;
    }

    async Task<long> FetchServerTime(CancellationToken cancellationToken)
    {
        Log.MinorAction($"Fetching server time");

        using HttpRequestMessage request = new(HttpMethod.Get, new Uri($"https://open.spotify.com/api/server-time", UriKind.Absolute));
        request.Version = new Version(2, 0);
        request.Headers.Clear();
        request.Headers.Add("User-Agent", userAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));

        HttpResponseMessage res = await client.SendAsync(request, cancellationToken: cancellationToken);
        res.EnsureSuccessStatusCode();

        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken: cancellationToken);

        return (body.Deserialize<ServerTimeResponse>() ?? throw new JsonException()).ServerTime;
    }
    const int TotpPeriod = 30;
    const int TotpDigits = 6;
    const string ToptVersion = "61";
    readonly byte[] ToptSecret = [51, 55, 54, 49, 51, 54, 51, 56, 55, 53, 51, 56, 52, 53, 57, 56, 57, 51, 56, 56, 51, 51, 49, 50, 51, 49, 48, 57, 49, 49, 57, 57, 50, 56, 52, 55, 49, 49, 50, 52, 52, 56, 56, 57, 52, 52, 49, 48, 50, 49, 48, 53, 49, 49, 50, 57, 55, 49, 48, 56];

    string GenerateTotp(long timestamp)
    {
        if (ToptSecret is null) throw new InvalidOperationException($"{nameof(ToptSecret)} is not set");

        long counter = timestamp / TotpPeriod;

        Span<byte> counterBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);

        Span<byte> hmacResult = stackalloc byte[20];
        HMACSHA1.HashData(ToptSecret, counterBytes, hmacResult);

        int offset = hmacResult[^1] & 0x0F;
        int binary = ((hmacResult[offset] & 0x7F) << 24) |
                     ((hmacResult[offset + 1] & 0xFF) << 16) |
                     ((hmacResult[offset + 2] & 0xFF) << 8) |
                     (hmacResult[offset + 3] & 0xFF);

        int otp = binary % (int)Math.Pow(10, TotpDigits);
        return otp.ToString($"D{TotpDigits}");
    }

    async Task<SessionTokenResponse> FetchSessionToken(CancellationToken cancellationToken)
    {
        long serverTime = await FetchServerTime(cancellationToken);

        string generatedTotp = GenerateTotp(serverTime);

        Log.MinorAction($"Fetching token");

        using HttpRequestMessage request = new(HttpMethod.Get, new Uri($"https://open.spotify.com/api/token?reason=init&productType=web-player&totp={generatedTotp}&totpServer={generatedTotp}&totpVer={ToptVersion}", UriKind.Absolute));
        PrepareXHRRequest(request);
        request.Version = new Version(2, 0);
        request.Headers.Clear();
        request.Headers.Add("User-Agent", userAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));

        HttpResponseMessage res = await client.SendAsync(request, cancellationToken: cancellationToken);
        string body = await res.Content.ReadAsStringAsync(cancellationToken);
        res.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<SessionTokenResponse>(body) ?? throw new JsonException();
    }

    async Task InitWs(CancellationToken cancellationToken = default)
    {
        Log.MinorAction($"Initializing websocket");

        if (accessToken is null) throw new InvalidOperationException($"{nameof(accessToken)} is not set");

        ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("User-Agent", userAgent);
        await ws.ConnectAsync(new Uri($"wss://gew1-dealer.g2.spotify.com/?access_token={accessToken}"), cancellationToken);

        await ws.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { type = "ping" })), WebSocketMessageType.Text, false, cancellationToken);

        _ = Task.Run(async () =>
        {
            byte[] buffer = new byte[1024];
            while (!cancellationToken.IsCancellationRequested)
            {
                WebSocketReceiveResult res = await ws.ReceiveAsync(buffer, cancellationToken);
                if (res.MessageType == WebSocketMessageType.Text)
                {
                    string msg = Encoding.UTF8.GetString(buffer.AsSpan(0, res.Count));
                    Log.Debug(msg);
                }
                else
                {
                    ReadOnlySpan<byte> msg = buffer.AsSpan(0, res.Count);
                    Log.Debug(Base64.EncodeToString(msg));
                }
            }
        }, cancellationToken);
    }

    async Task<ClientTokenResponse> FetchClientToken(string deviceId, CancellationToken cancellationToken = default)
    {
        Log.MinorAction($"Fetching client token");

        if (clientId is null) throw new InvalidOperationException($"{nameof(clientId)} is not set");
        if (appVersion is null) throw new InvalidOperationException($"{nameof(appVersion)} is not set");

        using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"https://clienttoken.spotify.com/v1/clienttoken", UriKind.Absolute));
        request.Version = new Version(3, 0);
        request.Headers.Clear();
        request.Headers.Add("User-Agent", userAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));

        request.Content = JsonContent.Create(new
        {
            client_data = new
            {
                client_version = "1.3.6.151.g40f63da325b7",
                client_id = "d8a5ed958d274c2e8ee717e6a4b0971d",
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
        }, new MediaTypeHeaderValue(MediaTypeNames.Application.Json, null));

        HttpResponseMessage res = await client.SendAsync(request, cancellationToken: cancellationToken);
        res.EnsureSuccessStatusCode();
        JsonElement body = await res.Content.ReadAsJsonAsync(cancellationToken);

        return body.Deserialize<ClientTokenResponse>() ?? throw new JsonException();
    }

    async Task AuthorizeIfRequired(CancellationToken cancellationToken = default)
    {
        string spotifyTokenFile = Path.Combine(arguments.HttpCachePath, "spotify-token.json");

        //if (accessToken is null || tokenExpiresAt <= DateTimeOffset.UtcNow)
        //{
        //    ExchangeDeviceCodeResponse token = await GetToken(cancellationToken);
        //    tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
        //
        //    Directory.CreateDirectory(Path.GetDirectoryName(spotifyTokenFile)!);
        //    File.WriteAllText(spotifyTokenFile, JsonSerializer.Serialize(new SavedClientToken()
        //    {
        //        Token = token,
        //        ExpiresAt = tokenExpiresAt,
        //    }));
        //
        //    accessToken = token.AccessToken;
        //    refreshToken = token.RefreshToken;
        //}

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

        //if (File.Exists(spotifyTokenFile))
        //{
        //    try
        //    {
        //        SavedClientToken r = JsonSerializer.Deserialize<SavedClientToken>(File.ReadAllText(spotifyTokenFile)) ?? throw new JsonException();

        //        DateTimeOffset now = DateTimeOffset.UtcNow;
        //        if (r.SessionTokenExpiresAt < now && r.ClientTokenExpiresAt < now)
        //        {
        //            accessToken = r.SessionToken;
        //            tokenExpiresAt = r.SessionTokenExpiresAt;
        //            clientId = r.ClientId;
        //            clientToken = r.ClientToken;
        //            clientTokenExpiresAt = r.ClientTokenExpiresAt;
        //            clientTokenRefreshAt = r.ClientTokenRefreshAt;
        //        }
        //        return;
        //    }
        //    catch (JsonException) { }
        //}

        {
            await FetchPage(cancellationToken);

            SessionTokenResponse sessionToken = await FetchSessionToken(cancellationToken);
            accessToken = sessionToken.AccessToken;
            tokenExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(sessionToken.AccessTokenExpirationTimestampMs);
            clientId = sessionToken.ClientId;

            GrantedClientToken _clientToken = (await FetchClientToken(deviceId, cancellationToken)).GrantedToken;
            clientToken = _clientToken.Token;
            clientTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(_clientToken.ExpiresAfterSeconds);
            clientTokenRefreshAt = DateTimeOffset.UtcNow.AddSeconds(_clientToken.RefreshAfterSeconds);

            Directory.CreateDirectory(Path.GetDirectoryName(spotifyTokenFile)!);
            File.WriteAllText(spotifyTokenFile, JsonSerializer.Serialize(new SavedClientToken()
            {
                SessionToken = accessToken,
                ClientId = clientId,
                SessionTokenExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(sessionToken.AccessTokenExpirationTimestampMs),
                ClientToken = clientToken,
                ClientTokenExpiresAt = clientTokenExpiresAt,
                ClientTokenRefreshAt = clientTokenRefreshAt,
            }));
        }

        await AuthorizeIfRequired(cancellationToken);
    }

    public void Dispose()
    {
        client.Dispose();
        ws?.CloseAsync(WebSocketCloseStatus.NormalClosure, "meow", default).Wait();
        ws?.Dispose();
    }
}
