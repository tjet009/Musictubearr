using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.YtDlp;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource.SkyHook;
using NzbDrone.Core.Music;
using NzbDrone.Core.Profiles.Metadata;

namespace NzbDrone.Core.MetadataSource.YouTube
{
    public class YouTubeProxy : IProvideArtistInfo, ISearchForNewArtist, IProvideAlbumInfo, ISearchForNewAlbum, ISearchForNewEntity
    {
        private readonly IYtDlpService _ytDlp;
        private readonly IConfigService _configService;
        private readonly IArtistService _artistService;
        private readonly IAlbumService _albumService;
        private readonly IMetadataProfileService _metadataProfileService;
        private readonly Logger _logger;

        public YouTubeProxy(IYtDlpService ytDlp,
                            IConfigService configService,
                            IArtistService artistService,
                            IAlbumService albumService,
                            IMetadataProfileService metadataProfileService,
                            Logger logger)
        {
            _ytDlp = ytDlp;
            _configService = configService;
            _artistService = artistService;
            _albumService = albumService;
            _metadataProfileService = metadataProfileService;
            _logger = logger;
        }

        public HashSet<string> GetChangedArtists(DateTime startTime)
        {
            // YouTube has no SkyHook-style change feed; refresh/RSS handles discovery.
            return new HashSet<string>();
        }

        public HashSet<string> GetChangedAlbums(DateTime startTime)
        {
            return new HashSet<string>();
        }

        public Artist GetArtistInfo(string foreignArtistId, int metadataProfileId)
        {
            var channelId = YouTubeIds.ExtractChannelId(foreignArtistId);
            if (channelId.IsNullOrWhiteSpace())
            {
                throw new ArtistNotFoundException(foreignArtistId);
            }

            _logger.Debug("Getting YouTube artist/channel {0}", channelId);

            YtDlpEntry channel;
            try
            {
                channel = _ytDlp.GetChannel(channelId) ?? _ytDlp.GetChannelUploads(channelId, 1);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to fetch YouTube channel {0}", channelId);
                throw new ArtistNotFoundException(foreignArtistId);
            }

            if (channel == null)
            {
                throw new ArtistNotFoundException(foreignArtistId);
            }

            var metadata = MapArtistMetadata(channel, channelId);
            var artist = new Artist
            {
                Metadata = metadata,
                CleanName = Parser.Parser.CleanArtistName(metadata.Name),
                SortName = Parser.Parser.NormalizeTitle(metadata.Name)
            };

            var albums = new List<Album>();

            // Uploads album (recent videos)
            try
            {
                var uploads = _ytDlp.GetChannelUploads(channelId, 100);
                var uploadAlbum = MapUploadsAlbum(uploads, metadata, channelId);
                if (uploadAlbum != null && IsAlbumAllowed(uploadAlbum, metadataProfileId))
                {
                    albums.Add(uploadAlbum);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to fetch uploads for channel {0}", channelId);
            }

            // Playlists as albums
            try
            {
                var playlists = _ytDlp.GetChannelPlaylists(channelId);
                foreach (var playlist in playlists.Take(40))
                {
                    try
                    {
                        var full = _ytDlp.GetPlaylist(playlist.Id, 200);
                        var album = MapPlaylistAlbum(full ?? playlist, metadata);
                        if (album != null && IsAlbumAllowed(album, metadataProfileId))
                        {
                            albums.Add(album);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug(ex, "Skipping playlist {0}", playlist.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to fetch playlists for channel {0}", channelId);
            }

            artist.Albums = albums;
            return artist;
        }

        public Tuple<string, Album, List<ArtistMetadata>> GetAlbumInfo(string foreignAlbumId)
        {
            _logger.Debug("Getting YouTube album {0}", foreignAlbumId);

            if (YouTubeIds.IsUploadsAlbum(foreignAlbumId))
            {
                var channelId = YouTubeIds.ExtractChannelId(foreignAlbumId);
                var uploads = _ytDlp.GetChannelUploads(channelId, 200);
                var channelMeta = MapArtistMetadata(uploads, channelId);
                var album = MapUploadsAlbum(uploads, channelMeta, channelId);
                if (album != null)
                {
                    AttachArtist(album, channelMeta);
                }
                return Tuple.Create(YouTubeIds.Channel(channelId), album, new List<ArtistMetadata> { channelMeta });
            }

            var playlistId = YouTubeIds.ExtractPlaylistId(foreignAlbumId);
            var playlist = _ytDlp.GetPlaylist(playlistId, 500);
            if (playlist == null)
            {
                throw new AlbumNotFoundException(foreignAlbumId);
            }

            var artistId = playlist.ResolvedChannelId ?? playlist.UploaderId;
            var artistMetadata = MapArtistMetadata(playlist, artistId);
            var mapped = MapPlaylistAlbum(playlist, artistMetadata);
            if (mapped != null)
            {
                AttachArtist(mapped, artistMetadata);
            }
            return Tuple.Create(YouTubeIds.Channel(artistId), mapped, new List<ArtistMetadata> { artistMetadata });
        }

        public List<Artist> SearchForNewArtist(string title)
        {
            try
            {
                if (title.IsNullOrWhiteSpace())
                {
                    return new List<Artist>();
                }

                var lower = title.Trim();

                if (lower.StartsWith("yt:", StringComparison.OrdinalIgnoreCase) ||
                    lower.StartsWith("musictubearr:", StringComparison.OrdinalIgnoreCase) ||
                    lower.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
                    lower.StartsWith("UC", StringComparison.Ordinal) ||
                    lower.StartsWith("@"))
                {
                    string foreignId;
                    if (lower.StartsWith("yt:channel:", StringComparison.OrdinalIgnoreCase) ||
                        lower.StartsWith("yt:uploads:", StringComparison.OrdinalIgnoreCase))
                    {
                        foreignId = YouTubeIds.Channel(YouTubeIds.ExtractChannelId(title.Trim()));
                    }
                    else if (lower.StartsWith("musictubearr:", StringComparison.OrdinalIgnoreCase) ||
                             (lower.StartsWith("yt:", StringComparison.OrdinalIgnoreCase) && !lower.Contains("://")))
                    {
                        var raw = title.Trim().Split(new[] { ':' }, 2)[1].Trim();
                        foreignId = YouTubeIds.Channel(YouTubeIds.ExtractChannelId(raw) ?? raw);
                    }
                    else
                    {
                        foreignId = YouTubeIds.Channel(YouTubeIds.ExtractChannelId(title.Trim()) ?? title.Trim());
                    }

                    try
                    {
                        var artist = GetArtistInfo(foreignId, 1);
                        var existing = _artistService.FindById(artist.ForeignArtistId);
                        return new List<Artist> { existing ?? artist };
                    }
                    catch (ArtistNotFoundException)
                    {
                        return new List<Artist>();
                    }
                }

                var channels = _ytDlp.SearchChannels(title, 10);
                var results = new List<Artist>();

                foreach (var channel in channels)
                {
                    var channelId = channel.ResolvedChannelId ?? channel.Id;
                    if (channelId.IsNullOrWhiteSpace())
                    {
                        continue;
                    }

                    var foreignId = YouTubeIds.Channel(channelId);
                    var existing = _artistService.FindById(foreignId);
                    if (existing != null)
                    {
                        results.Add(existing);
                        continue;
                    }

                    var metadata = MapArtistMetadata(channel, channelId);
                    results.Add(new Artist
                    {
                        Metadata = metadata,
                        CleanName = Parser.Parser.CleanArtistName(metadata.Name),
                        SortName = Parser.Parser.NormalizeTitle(metadata.Name),
                        Albums = new List<Album>()
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "YouTube artist search failed for '{0}'", title);
                throw new SkyHookException("Search for '{0}' failed. Unable to communicate with YouTube/yt-dlp.", ex, title);
            }
        }

        public List<Album> SearchForNewAlbum(string title, string artist)
        {
            try
            {
                if (title.IsNullOrWhiteSpace())
                {
                    return new List<Album>();
                }

                var lower = title.Trim().ToLowerInvariant();
                if (lower.StartsWith("yt:") || lower.StartsWith("musictubearr:") || lower.Contains("list=") || lower.StartsWith("pl"))
                {
                    var id = lower.Contains(':') && !lower.Contains("://")
                        ? title.Split(new[] { ':' }, 2)[1].Trim()
                        : title;

                    try
                    {
                        var tuple = GetAlbumInfo(id.StartsWith("yt:") ? id : YouTubeIds.Playlist(id));
                        var existing = _albumService.FindById(tuple.Item2.ForeignAlbumId);
                        return new List<Album> { existing ?? tuple.Item2 };
                    }
                    catch (AlbumNotFoundException)
                    {
                        return new List<Album>();
                    }
                }

                var query = artist.IsNullOrWhiteSpace() ? title : $"{artist} {title}";
                var videos = _ytDlp.SearchVideos(query, 15);
                var albums = new List<Album>();

                foreach (var video in videos)
                {
                    var channelId = video.ResolvedChannelId;
                    var metadata = MapArtistMetadata(video, channelId);
                    var album = MapSingleVideoAlbum(video, metadata);
                    if (album != null)
                    {
                        AttachArtist(album, metadata);
                        albums.Add(album);
                    }
                }

                return albums;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "YouTube album search failed for '{0}'", title);
                throw new SkyHookException("Search for '{0}' failed. Unable to communicate with YouTube/yt-dlp.", ex, title);
            }
        }

        public List<Album> SearchForNewAlbumByRecordingIds(List<string> recordingIds)
        {
            return new List<Album>();
        }

        public List<object> SearchForNewEntity(string title)
        {
            var results = new List<object>();
            results.AddRange(SearchForNewArtist(title));
            results.AddRange(SearchForNewAlbum(title, null));
            return results;
        }

        private bool IsAlbumAllowed(Album album, int metadataProfileId)
        {
            var metadataProfile = _metadataProfileService.Exists(metadataProfileId)
                ? _metadataProfileService.Get(metadataProfileId)
                : _metadataProfileService.All().FirstOrDefault();

            if (metadataProfile == null)
            {
                return true;
            }

            var primaryTypes = new HashSet<string>(metadataProfile.PrimaryAlbumTypes.Where(s => s.Allowed).Select(s => s.PrimaryAlbumType.Name));
            var secondaryTypes = new HashSet<string>(metadataProfile.SecondaryAlbumTypes.Where(s => s.Allowed).Select(s => s.SecondaryAlbumType.Name));

            if (!primaryTypes.Contains(album.AlbumType))
            {
                return false;
            }

            if (!album.SecondaryTypes.Any())
            {
                return secondaryTypes.Contains("Studio");
            }

            return album.SecondaryTypes.Any(x => secondaryTypes.Contains(x.Name));
        }

        private static void AttachArtist(Album album, ArtistMetadata metadata)
        {
            album.ArtistMetadata = metadata;
            album.Artist = new Artist
            {
                Metadata = metadata,
                CleanName = Parser.Parser.CleanArtistName(metadata.Name),
                SortName = Parser.Parser.NormalizeTitle(metadata.Name)
            };
        }

        private static ArtistMetadata MapArtistMetadata(YtDlpEntry entry, string channelId)
        {
            var id = channelId.IsNullOrWhiteSpace() ? entry?.ResolvedChannelId : channelId;
            var name = entry?.ResolvedChannelName ?? id ?? "Unknown";

            var metadata = new ArtistMetadata
            {
                ForeignArtistId = YouTubeIds.Channel(id ?? name),
                Name = name,
                Overview = entry?.Description ?? string.Empty,
                Type = "YouTube Channel",
                Status = ArtistStatusType.Continuing,
                Genres = new List<string> { "YouTube" },
                Ratings = new Ratings(),
                Images = new List<MediaCover.MediaCover>(),
                Links = new List<Links>
                {
                    new Links { Name = "youtube", Url = YouTubeIds.ToChannelUrl(id ?? name) }
                }
            };

            var thumb = entry?.BestThumbnail;
            if (thumb.IsNotNullOrWhiteSpace())
            {
                metadata.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Poster, thumb));
            }

            return metadata;
        }

        private Album MapUploadsAlbum(YtDlpEntry uploads, ArtistMetadata artistMetadata, string channelId)
        {
            if (uploads == null)
            {
                return null;
            }

            var entries = uploads.Entries ?? new List<YtDlpEntry>();
            if (entries.Count == 0 && uploads.Id.IsNotNullOrWhiteSpace() && uploads.Type != "playlist")
            {
                entries = new List<YtDlpEntry> { uploads };
            }

            var foreignAlbumId = YouTubeIds.Uploads(channelId);
            return BuildAlbum(
                foreignAlbumId,
                "Uploads",
                uploads.Description,
                "Other",
                artistMetadata,
                entries,
                ParseDate(uploads));
        }

        private Album MapPlaylistAlbum(YtDlpEntry playlist, ArtistMetadata artistMetadata)
        {
            if (playlist == null || playlist.Id.IsNullOrWhiteSpace())
            {
                return null;
            }

            var entries = playlist.Entries ?? new List<YtDlpEntry>();
            var title = playlist.Title.IsNullOrWhiteSpace() ? "Playlist" : playlist.Title;
            var albumType = GuessAlbumType(title, entries.Count);

            return BuildAlbum(
                YouTubeIds.Playlist(playlist.Id),
                title,
                playlist.Description,
                albumType,
                artistMetadata,
                entries,
                ParseDate(playlist));
        }

        private Album MapSingleVideoAlbum(YtDlpEntry video, ArtistMetadata artistMetadata)
        {
            if (video == null || video.Id.IsNullOrWhiteSpace())
            {
                return null;
            }

            return BuildAlbum(
                YouTubeIds.Playlist("single-" + video.Id),
                video.Title,
                video.Description,
                "Single",
                artistMetadata,
                new List<YtDlpEntry> { video },
                ParseDate(video));
        }

        private Album BuildAlbum(
            string foreignAlbumId,
            string title,
            string overview,
            string albumType,
            ArtistMetadata artistMetadata,
            List<YtDlpEntry> entries,
            DateTime? releaseDate)
        {
            var tracks = new List<Track>();
            var trackNumber = 1;

            var videoEntries = YouTubeContentFilter.FilterVideos(entries, _configService);

            foreach (var entry in videoEntries)
            {
                var durationMs = entry.Duration.HasValue ? (int)(entry.Duration.Value * 1000) : 0;
                tracks.Add(new Track
                {
                    ForeignTrackId = YouTubeIds.Video(entry.Id),
                    ForeignRecordingId = YouTubeIds.Video(entry.Id),
                    Title = entry.Title.IsNullOrWhiteSpace() ? entry.Id : entry.Title,
                    TrackNumber = trackNumber.ToString(),
                    AbsoluteTrackNumber = trackNumber,
                    Duration = durationMs,
                    MediumNumber = 1,
                    ArtistMetadata = artistMetadata
                });
                trackNumber++;
            }

            if (tracks.Count == 0)
            {
                return null;
            }

            var release = new AlbumRelease
            {
                ForeignReleaseId = YouTubeIds.Release(foreignAlbumId),
                Title = title,
                Status = "Official",
                ReleaseDate = releaseDate,
                Monitored = true,
                TrackCount = tracks.Count,
                Duration = tracks.Sum(t => t.Duration),
                Media = new List<Medium> { new Medium { Name = "Digital Media", Number = 1, Format = "Digital Media" } },
                Tracks = tracks
            };

            var album = new Album
            {
                ForeignAlbumId = foreignAlbumId,
                Title = title,
                Overview = overview ?? string.Empty,
                ReleaseDate = releaseDate,
                AlbumType = albumType,
                SecondaryTypes = new List<SecondaryAlbumType>(),
                CleanTitle = Parser.Parser.CleanArtistName(title),
                AnyReleaseOk = true,
                AlbumReleases = new List<AlbumRelease> { release },
                Images = new List<MediaCover.MediaCover>(),
                Links = new List<Links>
                {
                    new Links
                    {
                        Name = "youtube",
                        Url = YouTubeIds.IsUploadsAlbum(foreignAlbumId)
                            ? YouTubeIds.ToUploadsUrl(YouTubeIds.ExtractChannelId(foreignAlbumId))
                            : YouTubeIds.ToPlaylistUrl(foreignAlbumId)
                    }
                },
                Genres = new List<string> { "YouTube" },
                Ratings = new Ratings(),
                ArtistMetadata = artistMetadata
            };

            var thumb = entries.Select(e => e.BestThumbnail).FirstOrDefault(t => t.IsNotNullOrWhiteSpace());
            if (thumb.IsNotNullOrWhiteSpace())
            {
                album.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Cover, thumb));
            }

            return album;
        }

        private static string GuessAlbumType(string title, int trackCount)
        {
            var lower = title?.ToLowerInvariant() ?? string.Empty;
            if (lower.Contains("ep") && trackCount <= 6)
            {
                return "EP";
            }

            if (trackCount <= 3 || lower.Contains("single"))
            {
                return "Single";
            }

            if (lower.Contains("live") || lower.Contains("concert"))
            {
                return "Album";
            }

            return trackCount >= 4 ? "Album" : "Other";
        }

        private static DateTime? ParseDate(YtDlpEntry entry)
        {
            if (entry?.Timestamp != null)
            {
                return DateTimeOffset.FromUnixTimeSeconds(entry.Timestamp.Value).UtcDateTime;
            }

            if (entry?.UploadDate.IsNotNullOrWhiteSpace() == true &&
                DateTime.TryParseExact(entry.UploadDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
            {
                return date;
            }

            return null;
        }
    }
}
