using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.Download.YtDlp
{
    public class YtDlpEntry
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("duration")]
        public double? Duration { get; set; }

        [JsonProperty("timestamp")]
        public long? Timestamp { get; set; }

        [JsonProperty("upload_date")]
        public string UploadDate { get; set; }

        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("channel_id")]
        public string ChannelId { get; set; }

        [JsonProperty("uploader")]
        public string Uploader { get; set; }

        [JsonProperty("uploader_id")]
        public string UploaderId { get; set; }

        [JsonProperty("uploader_url")]
        public string UploaderUrl { get; set; }

        [JsonProperty("channel_url")]
        public string ChannelUrl { get; set; }

        [JsonProperty("thumbnail")]
        public string Thumbnail { get; set; }

        [JsonProperty("thumbnails")]
        public List<YtDlpThumbnail> Thumbnails { get; set; }

        [JsonProperty("webpage_url")]
        public string WebpageUrl { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("_type")]
        public string Type { get; set; }

        [JsonProperty("ie_key")]
        public string IeKey { get; set; }

        [JsonProperty("playlist_count")]
        public int? PlaylistCount { get; set; }

        [JsonProperty("entries")]
        public List<YtDlpEntry> Entries { get; set; }

        [JsonProperty("availability")]
        public string Availability { get; set; }

        [JsonProperty("categories")]
        public List<string> Categories { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        public string BestThumbnail
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Thumbnail))
                {
                    return Thumbnail;
                }

                if (Thumbnails == null || Thumbnails.Count == 0)
                {
                    return null;
                }

                return Thumbnails[Thumbnails.Count - 1].Url;
            }
        }

        public string ResolvedChannelId =>
            !string.IsNullOrWhiteSpace(ChannelId) ? ChannelId :
            !string.IsNullOrWhiteSpace(UploaderId) && UploaderId.StartsWith("UC") ? UploaderId :
            null;

        public string ResolvedChannelName =>
            !string.IsNullOrWhiteSpace(Channel) ? Channel :
            !string.IsNullOrWhiteSpace(Uploader) ? Uploader :
            Title;
    }

    public class YtDlpThumbnail
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }

        [JsonProperty("width")]
        public int? Width { get; set; }
    }
}
