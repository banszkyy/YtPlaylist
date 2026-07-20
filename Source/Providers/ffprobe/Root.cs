// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
using System.Text.Json.Serialization;

namespace FFMPEG.Probe;

public class Root
{
    [JsonPropertyName("streams")]
    public List<Stream>? Streams { get; set; }

    [JsonPropertyName("format")]
    public Format? Format { get; set; }
}

