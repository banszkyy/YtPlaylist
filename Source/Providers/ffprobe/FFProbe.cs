using System.Diagnostics;
using System.Text.Json;
using Logger;

namespace YtPlaylist.FFMPEG.Probe;

static class FFProbe
{
    public static async Task<FFProbeResult?> Probe(string filename, CancellationToken cancellationToken = default)
    {
        using Process? process = Process.Start(new ProcessStartInfo()
        {
            FileName = "ffprobe",
            Arguments = $"-v quiet -show_streams -show_format -print_format json \"{filename}\"",
            RedirectStandardOutput = true,
        });

        if (process is null)
        {
            Log.Error($"Failed to start ffplay");
            return null;
        }
        else
        {
            await process.WaitForExitAsync(cancellationToken);
        }

        string res = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        return JsonSerializer.Deserialize<FFProbeResult>(res);
    }
}
