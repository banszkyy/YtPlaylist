using System.Diagnostics;
using Logger;
using YtPlaylist;

static class YtDlp
{
    public static void DownloadAudioData(string filename, string url)
    {
        using (Log.Auto())
        {
            using Process process = Process.Start(new ProcessStartInfo()
            {
                FileName = "yt-dlp",
                Arguments = $"--output \"{filename}\" --extract-audio --audio-format mp3 {url}",
                UseShellExecute = true,
            })!;
            process.WaitForExit();
            if (process.ExitCode != 0) throw new YtDlpExceptionException(process.ExitCode);
        }
    }
}
