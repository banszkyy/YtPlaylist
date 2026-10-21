using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Web;
using YtPlaylist.Har;

namespace YtPlaylist;

sealed class HarLogger(string filename) : IDisposable
{
    readonly List<Entry> entries = [];
    readonly List<Page> pages = [];

    public sealed class HarPageLogger(Page page, List<Entry> entries) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken cancellationToken)
        {
            string reqUrl = req.RequestUri!.ToString();
            if (reqUrl.Contains('#')) reqUrl = reqUrl[..reqUrl.IndexOf('#')];

            List<NameValuePair> reqCookies = [];
            List<NameValuePair> reqHeaders = Headers(req.Headers, req.Content?.Headers);
            List<NameValuePair> reqQuery = Query(req.RequestUri);

            if (req.Headers.TryGetValues("Cookie", out IEnumerable<string>? cookieValues))
            {
                foreach (string cookieValue in cookieValues)
                {
                    foreach (string item in cookieValue.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    {
                        int i = item.IndexOf('=');
                        string name = HttpUtility.UrlDecode(i == -1 ? item : item[..i]);
                        string value = i == -1 ? string.Empty : HttpUtility.UrlDecode(item[(i + 1)..]);
                        reqCookies.RemoveAll(v => v.Name == name);
                        reqCookies.Add(new(name, value));
                    }
                }
            }

            if (req.RequestUri.IsAbsoluteUri && req.RequestUri.Query.Length > 1)
            {
                foreach (string item in req.RequestUri.Query[1..].Split('&'))
                {
                    int i = item.IndexOf('=');
                    string name = HttpUtility.UrlDecode(i == -1 ? item : item[..i]);
                    string value = i == -1 ? string.Empty : HttpUtility.UrlDecode(item[(i + 1)..]);
                    reqQuery.Add(new(name, value));
                }
            }

            DateTimeOffset reqTime = DateTimeOffset.UtcNow;
            Stopwatch sw = Stopwatch.StartNew();

            string? reqText = null;
            string reqMime = "";
            long reqBodySize = 0;
            if (req.Content is { } rc)
            {
                await rc.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
                reqMime = rc.Headers.ContentType?.MediaType ?? "";
                byte[] bytes = await rc.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                reqBodySize = bytes.Length;
                if (IsTextual(reqMime)) reqText = Encoding.UTF8.GetString(bytes);
            }

            long tSend = sw.ElapsedMilliseconds;
            sw.Restart();

            HttpResponseMessage res;
            try
            {
                res = await base.SendAsync(req, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                long tWait1 = sw.ElapsedMilliseconds;

                // Record failures too: a HAR that omits transport errors is misleading.
                entries.Add(new()
                {
                    StartedDateTime = reqTime.ToOffset(DateTimeOffset.Now.Offset),
                    Timings = new()
                    {
                        Blocked = 0,
                        Dns = 0,
                        Connect = 0,
                        Ssl = 0,
                        Send = tSend,
                        Wait = tWait1,
                        Receive = 0,
                    },
                    Time = tSend + tWait1,
                    Request = new()
                    {
                        Method = req.Method.ToString(),
                        Url = reqUrl,
                        HttpVersion = $"HTTP/{req.Version}",
                        Cookies = reqCookies,
                        Headers = reqHeaders,
                        QueryString = reqQuery,
                        HeadersSize = 0,
                        BodySize = reqBodySize,
                        PostData = reqText is null ? null : new()
                        {
                            MimeType = reqMime,
                            Params = [],
                            Text = reqText,
                        }
                    },
                    Response = new()
                    {
                        Status = 0,
                        StatusText = "Transport error",
                        HttpVersion = $"HTTP/1.1",
                        Cookies = [],
                        Headers = [],
                        Content = new(),
                        BodySize = 0,
                        HeadersSize = 0,
                        RedirectUrl = string.Empty,
                    },
                    Pageref = page.Id,
                    Cache = new()
                    {

                    }
                });
                throw;
            }

            long tWait = sw.ElapsedMilliseconds;

            string? resText = null;
            string resMime = "";
            long resBodySize = 0;
            string? resEncoding = null;

            sw.Restart();
            if (res.Content is { } c)
            {
                await c.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
                resMime = c.Headers.ContentType?.MediaType ?? "";
                byte[] bytes = await c.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                resBodySize = bytes.Length;
                if (IsTextual(resMime))
                {
                    resText = Encoding.UTF8.GetString(bytes);
                }
                else
                {
                    resText = Convert.ToBase64String(bytes);
                    resEncoding = "base64";
                }
            }
            long tReceive = sw.ElapsedMilliseconds;

            List<NameValuePair> resCookies = [];
            List<NameValuePair> resHeaders = Headers(res.Headers, res.Content?.Headers);

            if (res.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues))
            {
                foreach (string setCookieValue in setCookieValues)
                {
                    foreach (string item in setCookieValue.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    {
                        int i = item.IndexOf('=');
                        string name = HttpUtility.UrlDecode(i == -1 ? item : item[..i]);
                        string value = i == -1 ? string.Empty : HttpUtility.UrlDecode(item[(i + 1)..]);
                        resCookies.RemoveAll(v => v.Name == name);
                        resCookies.Add(new(name, value));
                    }
                }
            }

            entries.Add(new Entry()
            {
                StartedDateTime = reqTime.ToOffset(DateTimeOffset.Now.Offset),
                Timings = new()
                {
                    Blocked = 0,
                    Dns = 0,
                    Ssl = 0,
                    Connect = 0,
                    Send = tSend,
                    Wait = tWait,
                    Receive = tReceive,
                },
                Time = tSend + tWait + tReceive,
                Request = new()
                {
                    Method = req.Method.ToString(),
                    Url = reqUrl,
                    HttpVersion = $"HTTP/{req.Version}",
                    Cookies = reqCookies,
                    Headers = reqHeaders,
                    QueryString = reqQuery,
                    HeadersSize = 0,
                    BodySize = reqBodySize,
                    PostData = reqText is null ? null : new()
                    {
                        MimeType = reqMime,
                        Params = [],
                        Text = reqText,
                    }
                },
                Response = new()
                {
                    Status = (long)res.StatusCode,
                    StatusText = res.ReasonPhrase ?? string.Empty,
                    HttpVersion = $"HTTP/{res.Version}",
                    Cookies = resCookies,
                    Headers = resHeaders,
                    Content = new()
                    {
                        MimeType = resMime,
                        Size = resBodySize,
                        Encoding = resEncoding,
                        Text = resText,
                    },
                    BodySize = resBodySize,
                    HeadersSize = 0,
                    RedirectUrl = res.Headers.Location?.ToString() ?? string.Empty,
                },
                Pageref = page.Id,
                Cache = new()
                {

                }
            });
            return res;
        }

        static bool IsTextual(string mime) =>
           mime.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
           mime.Contains("json", StringComparison.OrdinalIgnoreCase) ||
           mime.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
           mime.Contains("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);

        static List<NameValuePair> Headers(System.Net.Http.Headers.HttpHeaders headers, System.Net.Http.Headers.HttpHeaders? content)
        {
            List<NameValuePair> list = [];
            foreach (KeyValuePair<string, IEnumerable<string>> kv in headers)
            {
                list.Add(new(kv.Key, string.Join(", ", kv.Value)));
            }

            if (content is not null)
            {
                foreach (KeyValuePair<string, IEnumerable<string>> kv in content)
                {
                    list.Add(new(kv.Key, string.Join(", ", kv.Value)));
                }
            }

            return list;
        }

        static List<NameValuePair> Query(Uri? uri)
        {
            if (uri is null || !uri.IsAbsoluteUri || uri.Query.Length < 2) return [];
            NameValueCollection q = HttpUtility.ParseQueryString(uri.Query);
            return [.. q.AllKeys.Where(k => k is not null).Select(k => new NameValuePair(k!, q[k] ?? ""))];
        }
    }

    public HarPageLogger BeginPage(string title)
    {
        Page? page = pages.FirstOrDefault(v => v.Title == title);
        if (page is not null) return new(page, entries);

        page = new()
        {
            StartedDateTime = DateTimeOffset.Now,
            Id = $"page_{pages.Count}",
            Title = title,
            PageTimings = new()
            {
                OnContentLoad = 0,
                OnLoad = 0,
            },
        };
        pages.Add(page);
        return new(page, entries);
    }

    public void Flush()
    {
        File.WriteAllText(filename, JsonSerializer.Serialize(new HarFile()
        {
            Log = new()
            {
                Version = "1.2",
                Creator = new()
                {
                    Name = Assembly.GetCallingAssembly().GetName().Name ?? "",
                    Version = "1.0.0",
                },
                Browser = new()
                {
                    Name = nameof(HttpClient),
                    Version = "1.0.0",
                },
                Entries = entries,
                Pages = pages,
            }
        }));
    }

    public void Dispose() => Flush();
}