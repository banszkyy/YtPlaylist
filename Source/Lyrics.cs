using Logger;
using TagLib.Id3v2;

namespace YtPlaylist;

static class Lyrics
{
    public static SynchedText[]? Parse(string synchedLyrics)
    {
        string[] lines = synchedLyrics.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        SynchedText[] synchedTexts = new SynchedText[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (!line.StartsWith('['))
            {
                Log.Error($"Invalid lyrics");
                return null;
            }

            int j = line.IndexOf(']');

            if (j == -1)
            {
                Log.Error($"Invalid lyrics");
                return null;
            }

            string time = line[1..j];
            line = line[(j + 1)..].TrimStart();

            string[] timeSegments = time.Split(':');
            if (timeSegments.Length != 2)
            {
                Log.Error($"Invalid lyrics");
                return null;
            }

            if (!int.TryParse(timeSegments[0], out int minute))
            {
                Log.Error($"Invalid lyrics");
                return null;
            }

            if (!double.TryParse(timeSegments[1], out double second))
            {
                Log.Error($"Invalid lyrics");
                return null;
            }

            synchedTexts[i] = new SynchedText(
                (long)TimeSpan.FromSeconds(second + (minute * 60d)).TotalMilliseconds,
                line
            );
        }

        return synchedTexts;
    }
}