using Google.Apis.YouTube.v3;

static class YouTubeUtils
{
    public static async Task<bool> RemoveFromPlaylist(YouTubeService client, string playlistId, string videoId, CancellationToken cancellationToken = default)
    {
        PlaylistItemsResource.ListRequest listRequest = client.PlaylistItems.List("id,snippet");
        listRequest.PlaylistId = playlistId;
        listRequest.VideoId = videoId;
        listRequest.MaxResults = 1;

        Google.Apis.YouTube.v3.Data.PlaylistItemListResponse listResponse = await listRequest.ExecuteAsync(cancellationToken);
        Google.Apis.YouTube.v3.Data.PlaylistItem? item = listResponse.Items?.FirstOrDefault();

        if (item == null)
        {
            return false;
        }

        PlaylistItemsResource.DeleteRequest deleteRequest = client.PlaylistItems.Delete(item.Id);
        await deleteRequest.ExecuteAsync(cancellationToken);

        return true;
    }
}