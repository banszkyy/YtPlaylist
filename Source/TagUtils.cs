using Logger;

namespace YtPlaylist;

static class TagUtils
{
    public static async Task<bool> DownloadCoverImage(TagLib.File file, Uri url, string description, TagLib.PictureType type, Diff diff, CancellationToken cancellationToken)
    {
        byte[]? imageBytes = null;
        string? mimeType = null;
        using (HttpClient client = new())
        {
            try
            {
                using (HttpResponseMessage res = await client.GetAsync(url, cancellationToken))
                {
                    if (!res.IsSuccessStatusCode) return false;
                    imageBytes = await res.Content.ReadAsByteArrayAsync(cancellationToken);
                    mimeType = res.Content.Headers.ContentType?.MediaType;
                }
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

        mimeType ??= Path.GetExtension(url.AbsolutePath) switch
        {
            ".jpg" => System.Net.Mime.MediaTypeNames.Image.Jpeg,
            ".jpeg" => System.Net.Mime.MediaTypeNames.Image.Jpeg,
            ".png" => System.Net.Mime.MediaTypeNames.Image.Png,
            ".bmp" => System.Net.Mime.MediaTypeNames.Image.Bmp,
            ".avif" => System.Net.Mime.MediaTypeNames.Image.Avif,
            ".gif" => System.Net.Mime.MediaTypeNames.Image.Gif,
            ".svg" => System.Net.Mime.MediaTypeNames.Image.Svg,
            ".tiff" => System.Net.Mime.MediaTypeNames.Image.Tiff,
            ".webp" => System.Net.Mime.MediaTypeNames.Image.Webp,
            _ => null,
        };

        if (mimeType is null or
            not System.Net.Mime.MediaTypeNames.Image.Jpeg
            and not System.Net.Mime.MediaTypeNames.Image.Png
            and not System.Net.Mime.MediaTypeNames.Image.Bmp
            and not System.Net.Mime.MediaTypeNames.Image.Avif
            and not System.Net.Mime.MediaTypeNames.Image.Gif
            and not System.Net.Mime.MediaTypeNames.Image.Svg
            and not System.Net.Mime.MediaTypeNames.Image.Tiff
            and not System.Net.Mime.MediaTypeNames.Image.Webp)
        {
            Log.Warning($"Unknown cover image media type \"{mimeType}\"");
            return false;
        }

        Log.None("Cover art downloaded");
        TagLib.Id3v2.AttachmentFrame cover = new()
        {
            Type = type,
            Description = description,
            MimeType = mimeType,
            Data = imageBytes,
            TextEncoding = TagLib.StringType.UTF16,
        };
        file.Tag.Pictures = diff.Modify("Pictures", file.Tag.Pictures, [cover]);
        return true;
    }
}