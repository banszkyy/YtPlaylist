using Logger;

namespace YtPlaylist;

static class TagUtils
{
    public static async Task<bool> DownloadCoverImage(TagLib.File file, Uri url, string description, TagLib.PictureType type, Diff diff, CancellationToken cancellationToken)
    {
        byte[]? imageBytes = null;
        using (HttpClient client = new())
        {
            try
            {
                imageBytes = await client.GetByteArrayAsync(url, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                if (!cancellationToken.IsCancellationRequested && ex.StatusCode != System.Net.HttpStatusCode.NotFound) Log.Error(ex);
                return false;
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested) Log.Error(ex);
                return false;
            }
        }

        Log.None("Cover art downloaded");
        TagLib.Id3v2.AttachmentFrame cover = new()
        {
            Type = type,
            Description = description,
            MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg,
            Data = imageBytes,
            TextEncoding = TagLib.StringType.UTF16,
        };
        file.Tag.Pictures = diff.Modify("Pictures", file.Tag.Pictures, [cover]);
        return true;
    }
}