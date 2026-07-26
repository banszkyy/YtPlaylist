using System.Text.Json.Serialization;

namespace YtPlaylist.SoundCloud;

public class DateOfBirth
{
    [JsonPropertyName("month")] public long Month { get; init; }
    [JsonPropertyName("year")] public long Year { get; init; }
    [JsonPropertyName("day")] public long Day { get; init; }

    public override string? ToString() => $"{Year}-{Month.ToString().PadLeft(2, '0')}-{Day.ToString().PadLeft(2, '0')}";
}
