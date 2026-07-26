namespace YtPlaylist.SoundCloud;

public enum DurationFilter
{
    Any,
    Short,
    Medium,
    Long,
    Epic,
}

public enum CreatedAtFilter
{
    Any,
    LastHour,
    LastDay,
    LastWeek,
    LastMonth,
    LastYear,
}

public enum LicenseFilter
{
    Any,
    ToModifyCommercially,
    ToShare,
    ToUseCommercially,
}


public class SearchRequestTrackFilter : SearchRequestFilter
{
    public string? GenreOrTag { get; set; }
    public string? Genre { get; set; }
    public DurationFilter Duration { get; set; }
    public CreatedAtFilter CreatedAt { get; set; }
    public LicenseFilter License { get; set; }
}
