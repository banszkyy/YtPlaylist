namespace YtPlaylist;

public enum ChangeType
{
    Create,
    Modify,
    Delete,
}

public readonly struct Change(MusicFile file, ChangeType type)
{
    public MusicFile File { get; } = file;
    public ChangeType Type { get; } = type;
}
