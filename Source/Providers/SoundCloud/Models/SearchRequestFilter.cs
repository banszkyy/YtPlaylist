namespace YtPlaylist.SoundCloud;

public class SearchRequestFilter
{
    public required string Query { get; set; }
    public int Limit { get; set; } = 10;
    public int Offset { get; set; } = 0;
}
