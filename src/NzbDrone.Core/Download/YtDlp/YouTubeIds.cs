using System;
using System.Web;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Download.YtDlp
{
    public static class YouTubeIds
    {
        public const string ChannelPrefix = "yt:channel:";
        public const string PlaylistPrefix = "yt:playlist:";
        public const string UploadsPrefix = "yt:uploads:";
        public const string VideoPrefix = "yt:video:";
        public const string ReleasePrefix = "yt:release:";

        public static string Channel(string channelId) => ChannelPrefix + StripPrefix(channelId);
        public static string Playlist(string playlistId) => PlaylistPrefix + StripPrefix(playlistId);
        public static string Uploads(string channelId) => UploadsPrefix + StripPrefix(channelId);
        public static string Video(string videoId) => VideoPrefix + StripPrefix(videoId);
        public static string Release(string albumForeignId) => ReleasePrefix + StripPrefix(albumForeignId);

        public static bool IsYouTubeId(string id)
        {
            return id.IsNotNullOrWhiteSpace() &&
                   (id.StartsWith(ChannelPrefix, StringComparison.OrdinalIgnoreCase) ||
                    id.StartsWith(PlaylistPrefix, StringComparison.OrdinalIgnoreCase) ||
                    id.StartsWith(UploadsPrefix, StringComparison.OrdinalIgnoreCase) ||
                    id.StartsWith(VideoPrefix, StringComparison.OrdinalIgnoreCase) ||
                    id.StartsWith(ReleasePrefix, StringComparison.OrdinalIgnoreCase) ||
                    id.StartsWith("UC", StringComparison.Ordinal) ||
                    id.StartsWith("PL", StringComparison.Ordinal) ||
                    id.StartsWith("UU", StringComparison.Ordinal) ||
                    id.StartsWith("yt:", StringComparison.OrdinalIgnoreCase) ||
                    id.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
                    id.Contains("youtu.be", StringComparison.OrdinalIgnoreCase));
        }

        public static string ExtractChannelId(string foreignId)
        {
            if (foreignId.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (foreignId.StartsWith(ChannelPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return foreignId.Substring(ChannelPrefix.Length);
            }

            if (foreignId.StartsWith(UploadsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return foreignId.Substring(UploadsPrefix.Length);
            }

            if (foreignId.StartsWith("UC", StringComparison.Ordinal))
            {
                return foreignId;
            }

            if (foreignId.Contains("youtube.com/channel/", StringComparison.OrdinalIgnoreCase))
            {
                var idx = foreignId.IndexOf("/channel/", StringComparison.OrdinalIgnoreCase) + "/channel/".Length;
                var rest = foreignId.Substring(idx);
                var slash = rest.IndexOf('/');
                return slash >= 0 ? rest.Substring(0, slash) : rest;
            }

            return foreignId;
        }

        public static string ExtractPlaylistId(string foreignId)
        {
            if (foreignId.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (foreignId.StartsWith(PlaylistPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return foreignId.Substring(PlaylistPrefix.Length);
            }

            if (foreignId.StartsWith(UploadsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var channelId = foreignId.Substring(UploadsPrefix.Length);
                return channelId.StartsWith("UC") ? string.Concat("UU", channelId.AsSpan(2)) : channelId;
            }

            if (foreignId.StartsWith("PL", StringComparison.Ordinal) || foreignId.StartsWith("UU", StringComparison.Ordinal) || foreignId.StartsWith("OL", StringComparison.Ordinal))
            {
                return foreignId;
            }

            if (foreignId.Contains("list=", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(foreignId.Contains("://") ? foreignId : "https://youtube.com/playlist?list=" + foreignId);
                var query = HttpUtility.ParseQueryString(uri.Query);
                return query.Get("list");
            }

            return foreignId;
        }

        public static string ExtractVideoId(string foreignId)
        {
            if (foreignId.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (foreignId.StartsWith(VideoPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return foreignId.Substring(VideoPrefix.Length);
            }

            if (foreignId.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase))
            {
                var idx = foreignId.IndexOf("youtu.be/", StringComparison.OrdinalIgnoreCase) + "youtu.be/".Length;
                var rest = foreignId.Substring(idx);
                var q = rest.IndexOf('?');
                return q >= 0 ? rest.Substring(0, q) : rest;
            }

            if (foreignId.Contains("v=", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(foreignId.Contains("://") ? foreignId : "https://youtube.com/watch?v=" + foreignId);
                var query = HttpUtility.ParseQueryString(uri.Query);
                return query.Get("v");
            }

            if (foreignId.Length == 11)
            {
                return foreignId;
            }

            return foreignId;
        }

        public static bool IsUploadsAlbum(string foreignAlbumId)
        {
            return foreignAlbumId.IsNotNullOrWhiteSpace() &&
                   foreignAlbumId.StartsWith(UploadsPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static string ToChannelUrl(string channelIdOrUrl)
        {
            if (channelIdOrUrl.IsNullOrWhiteSpace())
            {
                return channelIdOrUrl;
            }

            if (channelIdOrUrl.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
                channelIdOrUrl.StartsWith("@", StringComparison.Ordinal))
            {
                return channelIdOrUrl.StartsWith("@")
                    ? "https://www.youtube.com/" + channelIdOrUrl
                    : channelIdOrUrl;
            }

            var id = ExtractChannelId(channelIdOrUrl);
            return $"https://www.youtube.com/channel/{id}";
        }

        public static string ToUploadsUrl(string channelId)
        {
            var id = ExtractChannelId(channelId);
            return $"https://www.youtube.com/channel/{id}/videos";
        }

        public static string ToPlaylistsUrl(string channelId)
        {
            var id = ExtractChannelId(channelId);
            return $"https://www.youtube.com/channel/{id}/playlists";
        }

        public static string ToPlaylistUrl(string playlistIdOrUrl)
        {
            if (playlistIdOrUrl.IsNullOrWhiteSpace())
            {
                return playlistIdOrUrl;
            }

            if (playlistIdOrUrl.Contains("youtube.com", StringComparison.OrdinalIgnoreCase))
            {
                return playlistIdOrUrl;
            }

            var id = ExtractPlaylistId(playlistIdOrUrl);
            return $"https://www.youtube.com/playlist?list={id}";
        }

        public static string ToVideoUrl(string videoIdOrUrl)
        {
            if (videoIdOrUrl.IsNullOrWhiteSpace())
            {
                return videoIdOrUrl;
            }

            if (videoIdOrUrl.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
                videoIdOrUrl.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
            {
                return videoIdOrUrl;
            }

            var id = ExtractVideoId(videoIdOrUrl);
            return $"https://www.youtube.com/watch?v={id}";
        }

        private static string StripPrefix(string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return value;
            }

            foreach (var prefix in new[] { ChannelPrefix, PlaylistPrefix, UploadsPrefix, VideoPrefix, ReleasePrefix })
            {
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return value.Substring(prefix.Length);
                }
            }

            return value;
        }
    }
}
