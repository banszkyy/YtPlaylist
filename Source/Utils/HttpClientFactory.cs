using HttpCache;

namespace YtPlaylist;

static class HttpClientFactory
{
    public static HttpClient Create(string name, HarLogger? logger = null)
    {
        SocketsHttpHandler socketsHandler = new()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
        };

        if (logger is not null)
        {
            HarLogger.HarPageLogger pageLogger = logger.BeginPage(name);
            pageLogger.InnerHandler = socketsHandler;
            return new HttpClient(pageLogger);
        }
        else
        {
            return new HttpClient(socketsHandler);
        }
    }

    public static MeowHttpClient CreateMeow(string name, IRequestCache? cache = null, HarLogger? logger = null, Action<HttpClient>? clientConfig = null, Action<SocketsHttpHandler>? handlerConfig = null)
    {
        SocketsHttpHandler httpClientHandler = new()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            AllowAutoRedirect = true,
        };
        handlerConfig?.Invoke(httpClientHandler);

        if (logger is not null)
        {
            HarLogger.HarPageLogger pageLogger = logger.BeginPage(name);
            pageLogger.InnerHandler = httpClientHandler;
            HttpClient client = new(pageLogger);
            clientConfig?.Invoke(client);
            return new MeowHttpClient(client, cache, pageLogger);
        }
        else
        {
            HttpClient client = new(httpClientHandler);
            clientConfig?.Invoke(client);
            return new MeowHttpClient(client, cache);
        }
    }
}