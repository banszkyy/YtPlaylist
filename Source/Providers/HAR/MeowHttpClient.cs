using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using HttpCache;
using JsonExtensions.Http;

namespace YtPlaylist;

sealed class MeowHttpClient(HttpClient client, IRequestCache? cache = null, HarLogger.HarPageLogger? logger = null, int cooldownMs = 1500) : IDisposable
{
    public HttpClient Client => client;
    DateTimeOffset lastRequestTime;

    public Task<HttpResponseMessage> GetAsync(string uri, string? cacheKey = null, bool cached = false, CancellationToken cancellationToken = default)
        => SendAsync(new(HttpMethod.Get, uri), cacheKey, cached, cancellationToken);

    public Task<HttpResponseMessage> GetAsync(Uri uri, string? cacheKey = null, bool cached = false, CancellationToken cancellationToken = default)
        => SendAsync(new(HttpMethod.Get, uri), cacheKey, cached, cancellationToken);

    public async Task<string> GetStringAsync(string uri, string? cacheKey = null, bool cached = false, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage res = await SendAsync(new(HttpMethod.Get, uri), cacheKey, cached, cancellationToken);
        return await res.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<JsonElement> GetJsonAsync(string uri, string? cacheKey = null, bool cached = false, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage req = new(HttpMethod.Get, uri);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        HttpResponseMessage res = await SendAsync(req, cacheKey, cached, cancellationToken);
        return await res.Content.ReadAsJsonAsync(cancellationToken);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, string? cacheKey = null, bool cached = false, CancellationToken cancellationToken = default)
    {
        Debug.Assert(req.RequestUri is not null);

        cacheKey ??= CacheUtils.GetKey(req.RequestUri);

        if (cached && cache is not null && cache.TryGetCachedItem(cacheKey, out Stream? stream, out HttpStatusCode status))
        {
            HttpResponseMessage cachedRes = new(status)
            {
                RequestMessage = req,
                Content = new StreamContent(stream!),
            };
            //if (logger is not null) await logger.RecordCached(req, cachedRes, cancellationToken);
            return cachedRes;
        }
        else
        {
            if (lastRequestTime != default)
            {
                double ellapsedMilliseconds = (DateTimeOffset.UtcNow - lastRequestTime).TotalMilliseconds;
                if (ellapsedMilliseconds < cooldownMs)
                {
                    await Task.Delay(cooldownMs - (int)Math.Max(0, ellapsedMilliseconds), cancellationToken);
                }
            }

            lastRequestTime = DateTimeOffset.UtcNow;

            HttpResponseMessage res = await client.SendAsync(req, cancellationToken);

            Stream content = await res.Content.ReadAsStreamAsync(cancellationToken);

            cache?.Add(cacheKey, content, res.StatusCode);

            HttpResponseMessage res2 = new(res.StatusCode)
            {
                ReasonPhrase = res.ReasonPhrase,
                Version = res.Version,
                RequestMessage = res.RequestMessage,
                Content = new StreamContent(content),
            };

            foreach (KeyValuePair<string, IEnumerable<string>> item in res.Headers)
            {
                res2.Headers.TryAddWithoutValidation(item.Key, item.Value);
            }
            foreach (KeyValuePair<string, IEnumerable<string>> item in res.TrailingHeaders)
            {
                res2.TrailingHeaders.TryAddWithoutValidation(item.Key, item.Value);
            }

            return res2;
        }
    }

    public void Dispose()
    {
        client.Dispose();
    }
}
