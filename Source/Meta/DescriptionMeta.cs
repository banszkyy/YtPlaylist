using System.Text;

namespace YtPlaylist;

public struct DescriptionMeta
{
    public required string YouTubeId { get; set; }
    public string? SpotifyId { get; set; }
    public long SoundCloudId { get; set; }
    public string? MusicBrainzRecordingId { get; set; }

    public static DescriptionMeta Empty => new() { YouTubeId = string.Empty };

    public static DescriptionMeta Parse(string v)
    {
        if (string.IsNullOrWhiteSpace(v)) return Empty;
        string[] parts = v.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return Empty;
        DescriptionMeta res = new() { YouTubeId = null! };
        foreach (string part in parts)
        {
            switch (part[0])
            {
                case 'y':
                    res.YouTubeId = part[1..];
                    break;
                case 'r':
                    res.MusicBrainzRecordingId = part[1..];
                    break;
                case 's':
                    res.SoundCloudId = long.Parse(part[1..]);
                    break;
                case 'o':
                    res.SpotifyId = part[1..];
                    break;
                default:
                    return Empty;
            }
        }
        if (res.YouTubeId is null) return Empty;
        return res;
    }

    public override string ToString()
    {
        StringBuilder res = new();

        res.Append($"y{YouTubeId}");
        if (!string.IsNullOrEmpty(MusicBrainzRecordingId)) res.Append($";r{MusicBrainzRecordingId}");
        if (SoundCloudId != default) res.Append($";s{SoundCloudId}");
        if (!string.IsNullOrEmpty(SpotifyId)) res.Append($";o{SpotifyId}");

        return res.ToString();
    }
}
