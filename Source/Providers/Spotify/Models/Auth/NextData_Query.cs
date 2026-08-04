using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class NextData_Query
{
    [J("code")] public required string Code { get; set; }
    [J("flow_ctx")] public required string FlowCtx { get; set; }
}
