using System.Text.Json.Serialization;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Har;

public class HarFile
{
    [J("log")] public required Log Log { get; set; }
}

public class Log
{
    [J("version")] public required string Version { get; set; }
    [J("creator")] public required Browser Creator { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("browser")] public Browser? Browser { get; set; }
    [J("pages")] public required List<Page> Pages { get; set; }
    [J("entries")] public required List<Entry> Entries { get; set; }
}

public class Browser
{
    [J("name")] public required string Name { get; set; }
    [J("version")] public required string Version { get; set; }
}

public class Entry
{
    [J("startedDateTime")] public required DateTimeOffset StartedDateTime { get; set; }
    [J("request")] public required Request Request { get; set; }
    [J("response")] public required Response Response { get; set; }
    [J("cache")] public required Cache Cache { get; set; }
    [J("timings")] public required Timings Timings { get; set; }
    [J("time")] public required long Time { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("serverIPAddress")] public string? ServerIpAddress { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("connection")] public string? Connection { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("pageref")] public string? Pageref { get; set; }
}

public class Cache
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("beforeRequest")] public CacheState? BeforeRequest { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("afterRequest")] public CacheState? AfterRequest { get; set; }
}

public class Request
{
    [J("bodySize")] public required long BodySize { get; set; }
    [J("method")] public required string Method { get; set; }
    [J("url")] public required string Url { get; set; }
    [J("httpVersion")] public required string HttpVersion { get; set; }
    [J("headers")] public required List<NameValuePair> Headers { get; set; }
    [J("cookies")] public required List<NameValuePair> Cookies { get; set; }
    [J("queryString")] public required List<NameValuePair> QueryString { get; set; }
    [J("headersSize")] public required long HeadersSize { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("postData")] public PostData? PostData { get; set; }
}

public class NameValuePair(string name, string value)
{
    [J("name")] public string Name { get; set; } = name;
    [J("value")] public string Value { get; set; } = value;
}

public class PostData
{
    [J("mimeType")] public required string MimeType { get; set; }
    [J("params")] public required List<object> Params { get; set; }
    [J("text")] public required string Text { get; set; }
}

public class Response
{
    [J("status")] public required long Status { get; set; }
    [J("statusText")] public required string StatusText { get; set; }
    [J("httpVersion")] public required string HttpVersion { get; set; }
    [J("headers")] public required List<NameValuePair> Headers { get; set; }
    [J("cookies")] public required List<NameValuePair> Cookies { get; set; }
    [J("content")] public required Content Content { get; set; }
    [J("redirectURL")] public required string RedirectUrl { get; set; }
    [J("headersSize")] public required long HeadersSize { get; set; }
    [J("bodySize")] public required long BodySize { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("comment")] public string? Comment { get; set; }
}

public class Content
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("mimeType")] public string? MimeType { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("size")] public long? Size { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("text")] public string? Text { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("encoding")] public string? Encoding { get; set; }
}

public class Timings
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("blocked")] public long? Blocked { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("dns")] public long? Dns { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("connect")] public long? Connect { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("ssl")] public long? Ssl { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("send")] public long? Send { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("wait")] public long? Wait { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("receive")] public long? Receive { get; set; }
}

public class Page
{
    [J("id")] public required string Id { get; set; }
    [J("pageTimings")] public required PageTimings PageTimings { get; set; }
    [J("startedDateTime")] public required DateTimeOffset StartedDateTime { get; set; }
    [J("title")] public required string Title { get; set; }
}

public class PageTimings
{
    [J("onContentLoad")] public required long OnContentLoad { get; set; }
    [J("onLoad")] public required long OnLoad { get; set; }
}

public class CacheState
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][J("expires")] public DateTimeOffset Expires { get; set; }
    [J("lastAccess")] public required DateTimeOffset LastAccess { get; set; }
    [J("eTag")] public required string ETag { get; set; }
    [J("hitCount")] public required long HitCount { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)][J("comment")] public string? Comment { get; set; }
}