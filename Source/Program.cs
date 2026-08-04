using System.Collections.Immutable;
using System.Text.Json;
using Logger;

namespace YtPlaylist;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            Console.WriteLine("YouTube Playlist Downloader");
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("ytsync <-p|--playlist Playlist Id> <-o|--output Output Directory>");
            return 1;
        }

        (AppArguments arguments, bool success) = AppArguments.Read(args);
        if (!success) return 1;

        CancellationTokenSource cancellationTokenSource = new();

        Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        new App(arguments).Run(cancellationTokenSource.Token).ContinueWith(task =>
        {
            if (task.Exception is not null)
            {
                foreach (Exception item in task.Exception.Flatten().InnerExceptions)
                {
                    Log.Error(item);
                }
            }
        }).Wait();

        return 0;
    }
}
