using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using AngleSharp;
using AngleSharp.Dom;
using HttpCache;
using JsonExtensions.Reading;

namespace YtPlaylist.SoundCloud;

sealed partial class SoundCloudClient : IDisposable
{
    readonly HttpClient client;
    readonly IRequestCache? cache;
    DateTimeOffset lastRequestTime;
    const int CooldownMilliseconds = 500;

    readonly string? token;
    readonly string? sessionId;
    readonly string? jspl;

    string? datadomeClientId;
    string? clientId;
    string? appVersion;
    long trackingUserId;
    string? trackingAnonymousId;

    readonly Dictionary<string, string> additionalCookies = [];

    public SoundCloudClient(SoundCloudCredentials credentials, ImmutableArray<NetscapeCookieFile.Cookie> cookies, IRequestCache? cache)
    {
        token = credentials.Token;
        jspl = credentials.Jspl;
        sessionId = credentials.SessionId;

        foreach (NetscapeCookieFile.Cookie cookie in cookies)
        {
            if (!cookie.Domain.EndsWith("soundcloud.com")) continue;
            switch (cookie.Name)
            {
                case "datadome":
                    this.datadomeClientId = HttpUtility.UrlDecode(cookie.Value);
                    break;
                case "oauth_token":
                    this.token = HttpUtility.UrlDecode(cookie.Value);
                    break;
                case "sc_session":
                    {
                        JsonDocument v = JsonDocument.Parse(HttpUtility.UrlDecode(cookie.Value));
                        this.sessionId = v.RootElement.GetProperty("id").GetStringOrNull();
                        break;
                    }
                case "sc_tracking_user_id":
                    {
                        this.trackingUserId = long.Parse(JsonElement.Parse(HttpUtility.UrlDecode(cookie.Value)).GetString()!.Split(':')[2]);
                        break;
                    }
                case "sc_tracking_anonymous_id":
                    {
                        this.trackingAnonymousId = JsonElement.Parse(HttpUtility.UrlDecode(cookie.Value)).GetString()!;
                        break;
                    }
            }

            additionalCookies[cookie.Name] = cookie.Value;
        }

        this.cache = cache;
        client = new()
        {
            BaseAddress = new Uri("https://api-v2.soundcloud.com")
        };
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("User-Agent", credentials.UserAgent ?? "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.3");
        //client.DefaultRequestHeaders.Add("Host", "api-v2.soundcloud.com");
        //client.DefaultRequestHeaders.Add("Origin", "https://soundcloud.com");
        //client.DefaultRequestHeaders.Add("Referer", "https://soundcloud.com/");
    }

    async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, bool cached, CancellationToken cancellationToken = default)
    {
        string cacheKey;
        if (request.RequestUri!.IsAbsoluteUri)
        {
            cacheKey = request.RequestUri!.ToString();
        }
        else
        {
            Uri absoluteUri = new(client.BaseAddress!, request.RequestUri!);
            StringBuilder cacheKeyBuilder = new();

            cacheKeyBuilder.Append(absoluteUri.LocalPath);
            List<string> queries = [];
            foreach (string item in absoluteUri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (item.IndexOf('=') != -1)
                {
                    string key = item[..item.IndexOf('=')];
                    if (key == "client_id") continue;
                    if (key == "app_version") continue;
                    if (key == "app_locale") continue;
                    if (key == "user_id") continue;
                    queries.Add(item);
                }
            }
            if (queries.Count > 0)
            {
                cacheKeyBuilder.Append('?');
                cacheKeyBuilder.AppendJoin('&', queries);
            }

            cacheKey = cacheKeyBuilder.ToString();
        }

        if (cached && cache is not null && await cache.TryGetCachedItem(cacheKey, out Stream? stream, out HttpStatusCode status))
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
            await cache.Add(cacheKey, content, res.StatusCode);

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

    string GetCookies()
    {
        Dictionary<string, string> cookies = [];
        FillCookies(cookies);
        return string.Join("; ", cookies.Select(v => $"{v.Key}={HttpUtility.UrlEncode(v.Value)}"));
    }

    void FillCookies(Dictionary<string, string> cookies)
    {
        static string FormatDate(DateTime d)
        {
            return d.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
        }

        cookies["sc_theme"] = "dark";
        if (!string.IsNullOrEmpty(sessionId)) cookies["sc_session"] = HttpUtility.UrlEncode(JsonSerializer.Serialize(new { id = sessionId, lastBecameInactive = FormatDate(DateTime.Now.AddDays(-1)) }));
        cookies["ja"] = "0";
        cookies["cookie_consent"] = "1";
        cookies["connect_session"] = "1";
        cookies["soundcloud_session_hint"] = "1";
        if (!string.IsNullOrEmpty(token)) cookies["oauth_token"] = HttpUtility.UrlEncode(token);
        if (!string.IsNullOrEmpty(datadomeClientId)) cookies["datadome"] = HttpUtility.UrlEncode(datadomeClientId);
        if (trackingUserId != default) cookies["sc_tracking_user_id"] = HttpUtility.UrlEncode($"\"soundcloud:users:{trackingUserId}\"");
        foreach (KeyValuePair<string, string> item in additionalCookies)
        {
            cookies[item.Key] = item.Value;
        }
    }

    void HandleResponse(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookies))
        {
            foreach (string item in setCookies)
            {
                string[] v = item.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                string key = v[0].Split('=')[0];
                string value = v[0].Split('=')[1];
                additionalCookies[key] = value;
            }
        }

        if (response.Headers.TryGetValues("x-set-cookie", out setCookies))
        {
            foreach (string item in setCookies)
            {
                string[] v = item.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                string key = v[0].Split('=')[0];
                string value = v[0].Split('=')[1];
                additionalCookies[key] = value;
            }
        }
    }

    class HydrationItem
    {
        [JsonPropertyName("hydratable")] public required string Hydratable { get; init; }
        [JsonPropertyName("data")] public required JsonElement Data { get; init; }
    }

    class HtmlPageMeta
    {
        public string? AppVersion { get; init; }
        public string? DdjsKey { get; init; }
        public ImmutableDictionary<string, JsonElement> Hydrations { get; init; } = [];
    }

    static HtmlPageMeta ParseHtmlPage(IDocument document)
    {
        string? appVersion = null;
        string? ddjsKey = null;
        ImmutableDictionary<string, JsonElement> hydrations = [];

        IHtmlCollection<IElement> scripts = document!.QuerySelectorAll("script");
        foreach (IElement item in scripts)
        {
            string script = item.InnerHtml.Trim();

            Match match = new Regex(@"window\.__sc_version\s*=\s*""(.*)""").Match(script);
            if (match.Success)
            {
                appVersion = match.Groups[1].Value;
                continue;
            }

            match = new Regex(@"window\.ddjskey\s*=\s*'(.*)';").Match(script);
            if (match.Success)
            {
                ddjsKey = match.Groups[1].Value;
                continue;
            }

            match = new Regex(@"window\.__sc_hydration\s*=\s*(.*);").Match(script);
            if (match.Success)
            {
                hydrations = JsonSerializer.Deserialize<ImmutableArray<HydrationItem>>(match.Groups[1].Value).ToImmutableDictionary(v => v.Hydratable, v => v.Data);
                continue;
            }
        }

        return new HtmlPageMeta()
        {
            AppVersion = appVersion,
            DdjsKey = ddjsKey,
            Hydrations = hydrations,
        };
    }

    public async Task Initialize(CancellationToken cancellationToken = default)
    {
        string? ddjsKey = null;

        {
            using HttpRequestMessage request = new(HttpMethod.Get, new Uri("/discover", UriKind.Relative));
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

            HttpResponseMessage res = await SendRequest(request, false, cancellationToken);
            res.EnsureSuccessStatusCode();
            HandleResponse(res);

            string body = await res.Content.ReadAsStringAsync(cancellationToken);
            IConfiguration config = Configuration.Default;
            using IBrowsingContext context = BrowsingContext.New(config);
            using IDocument document = await context.OpenAsync(req => req.Content(body), cancel: cancellationToken);

            HtmlPageMeta pageMeta = ParseHtmlPage(document);

            appVersion = pageMeta.AppVersion;
            ddjsKey = pageMeta.DdjsKey;
            if (pageMeta.Hydrations.TryGetValue("anonymousId", out JsonElement _anonymousId) && _anonymousId.ValueKind == JsonValueKind.String) trackingAnonymousId = _anonymousId.GetString();
            if (pageMeta.Hydrations.TryGetValue("apiClient", out JsonElement _apiClient) && _apiClient.ValueKind == JsonValueKind.Object)
            {
                if (_apiClient.TryGetProperty("id", out JsonElement _apiClientId) && _apiClientId.ValueKind == JsonValueKind.String)
                {
                    clientId = _apiClientId.GetString();
                }
            }
            if (pageMeta.Hydrations.TryGetValue("statsigClientInitializeResponse", out JsonElement _statsigClientInitializeResponse) && _statsigClientInitializeResponse.ValueKind == JsonValueKind.Object)
            {
                if (_statsigClientInitializeResponse.TryGetProperty("user", out JsonElement _user))
                {
                    if (_statsigClientInitializeResponse.TryGetProperty("appVersion", out JsonElement _appVersion))
                    {
                        if (_appVersion.GetString() != appVersion) Debugger.Break();
                    }
                }
            }
            if (pageMeta.Hydrations.TryGetValue("meUser", out JsonElement _meUser))
            {
                Me meUser = _meUser.Deserialize<Me>() ?? throw new JsonException();
                trackingUserId = meUser.Id;
            }

            if (string.IsNullOrEmpty(appVersion)) throw new SoundCloudException($"{nameof(appVersion)} is null");
            if (string.IsNullOrEmpty(clientId)) throw new SoundCloudException($"{nameof(clientId)} is null");
            if (string.IsNullOrEmpty(trackingAnonymousId)) throw new SoundCloudException($"{nameof(trackingAnonymousId)} is null");
            if (string.IsNullOrEmpty(ddjsKey)) throw new SoundCloudException($"{nameof(ddjsKey)} is null");
        }

        if (!string.IsNullOrWhiteSpace(jspl))
        {
            using HttpRequestMessage request = new(HttpMethod.Post, new Uri("https://dwt.soundcloud.com/js/", UriKind.Absolute));
            request.Headers.Clear();
            request.Headers.Add("Cookie", GetCookies());
            request.Headers.Add("Host", "dwt.soundcloud.com");
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
            request.Headers.Add("Referer", "https://soundcloud.com/");
            request.Headers.Add("Origin", "https://soundcloud.com");
            request.Headers.Add("Sec-GPC", "1");
            request.Headers.Add("Sec-Fetch-Dest", "empty");
            request.Headers.Add("Sec-Fetch-Mode", "cors");
            request.Headers.Add("Sec-Fetch-Site", "same-site");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "Referer", HttpUtility.UrlEncode("https://soundcloud.com/discover") },
                { "cid", ".keep" },
                { "ddk", ddjsKey },
                { "ddv", "5.9.0" },
                { "eventCounters", "[]" },
                { "jsType", "ch" },
                { "jspl", jspl },
                { "request", HttpUtility.UrlEncode("/discover") },
                { "responsePage", "origin" },
            });

            HttpResponseMessage res = await SendRequest(request, false, cancellationToken);
            res.EnsureSuccessStatusCode();

            string body = await res.Content.ReadAsStringAsync(cancellationToken);
            JsonDocument doc = JsonDocument.Parse(body);
            string cookie = doc.RootElement.GetProperty("cookie").GetString()!;
            cookie = cookie.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
            if (cookie.Split('=')[0] != "datadome") throw new SoundCloudException($"Expected cookie is not \"datadome\"");
            datadomeClientId = cookie.Split('=')[1];
        }
    }

    string BuildQueryParameters(params IEnumerable<(string Key, object Value)> parameters)
    {
        StringBuilder res = new();

        if (!string.IsNullOrWhiteSpace(clientId)) parameters = parameters.Prepend(("client_id", clientId));
        if (!string.IsNullOrWhiteSpace(appVersion)) parameters = parameters.Append(("app_version", appVersion));
        if (!string.IsNullOrWhiteSpace(trackingAnonymousId)) parameters = parameters.Append(("user_id", trackingAnonymousId));
        parameters = parameters.Append(("app_locale", "en"));

        bool separator = false;
        foreach ((string key, object value) in parameters)
        {
            if (separator) res.Append('&');
            res.Append(HttpUtility.UrlEncode(key));
            res.Append('=');
            res.Append(HttpUtility.UrlEncode(value.ToString()));
            separator = true;
        }

        return res.ToString();
    }

    void PrepareXHRRequest(HttpRequestMessage request, bool authorize)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        //request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Text.JavaScript));
        //request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.01));
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
        //request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.9));
        if (authorize && !string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", token);
        if (!string.IsNullOrWhiteSpace(datadomeClientId)) request.Headers.Add("x-datadome-clientid", datadomeClientId);
        request.Headers.Add("Sec-GPC", "1");
        request.Headers.Add("Sec-Fetch-Dest", "empty");
        request.Headers.Add("Sec-Fetch-Mode", "cors");
        request.Headers.Add("Sec-Fetch-Site", "same-site");
        request.Headers.Add("Cookie", GetCookies());
    }

    public void Dispose() => client.Dispose();
}
