using System.Collections.Immutable;
using System.Diagnostics;
using Logger;

namespace YtPlaylist;

static class YtDlp
{
    public static void DownloadAudioData(string filename, string url, ImmutableArray<string> additionalArguments)
    {
        using (Log.Auto())
        {
            using Process process = Process.Start(new ProcessStartInfo()
            {
                FileName = "yt-dlp",
                Arguments = string.Join(' ', [
                    $"--output",
                    $"\"{filename}\"",
                    $"--extract-audio",
                    $"--audio-format",
                    "mp3",
                    ..additionalArguments,
                    url,
                ]),
                UseShellExecute = true,
            })!;
            process.WaitForExit();
            if (process.ExitCode != 0) throw new YtDlpExceptionException(process.ExitCode);
        }
    }
}
