namespace YtPlaylist;

sealed class YtDlpExceptionException(int exitCode) : Exception($"yt-dlp exited with code {exitCode}")
{
    public int ExitCode { get; } = exitCode;
}
