using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;

public static class YoutubeServiceFactory
{
    public static async Task<YouTubeService> CreateAsync(CancellationToken cancellationToken = default)
    {
        using FileStream stream = new("/home/bb/Projects/YtPlaylist/credentials.json", FileMode.Open, FileAccess.Read);
        return new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromStream(stream).Secrets,
            [YouTubeService.Scope.Youtube, YouTubeService.Scope.YoutubeUpload],
            "yt-playlist",
            cancellationToken,
            new FileDataStore("/home/bb/Projects/YtPlaylist/token_cache", true)
        ),
            ApplicationName = "YtPlaylist"
        });
    }
}