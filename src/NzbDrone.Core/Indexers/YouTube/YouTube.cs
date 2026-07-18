using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.YtDlp;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.YouTube
{
    public class YouTube : IndexerBase<YouTubeSettings>
    {
        private readonly IYtDlpService _ytDlp;
        private readonly IArtistService _artistService;

        public override string Name => "YouTube";
        public override string Protocol => nameof(YouTubeDownloadProtocol);
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;

        public YouTube(IYtDlpService ytDlp,
                       IArtistService artistService,
                       IIndexerStatusService indexerStatusService,
                       IConfigService configService,
                       IParsingService parsingService,
                       Logger logger)
            : base(indexerStatusService, configService, parsingService, logger)
        {
            _ytDlp = ytDlp;
            _artistService = artistService;
        }

        public override async Task<IList<ReleaseInfo>> FetchRecent()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var releases = new List<ReleaseInfo>();

                    // Poll monitored YouTube channels for new uploads (RSS equivalent).
                    var artists = _artistService.GetAllArtists()
                        .Where(a => a.Monitored && a.ForeignArtistId.IsNotNullOrWhiteSpace() && YouTubeIds.IsYouTubeId(a.ForeignArtistId))
                        .Take(25)
                        .ToList();

                    foreach (var artist in artists)
                    {
                        try
                        {
                            var channelId = YouTubeIds.ExtractChannelId(artist.ForeignArtistId);
                            var uploads = _ytDlp.GetChannelUploads(channelId, Math.Min(Settings.MaxResults, 15), Settings.CookiesPath, Settings.YtDlpPath);
                            releases.AddRange(MapPlaylistReleases(uploads, artist.Name, "Uploads"));
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug(ex, "YouTube RSS poll failed for {0}", artist);
                        }
                    }

                    // Fallback so the indexer still returns something when no artists are monitored yet.
                    if (releases.Count == 0)
                    {
                        var videos = _ytDlp.SearchVideos("official audio", Math.Min(Settings.MaxResults, 10), Settings.CookiesPath, Settings.YtDlpPath);
                        releases.AddRange(videos.Select(v => MapRelease(v)));
                    }

                    _indexerStatusService.RecordSuccess(Definition.Id);
                    return CleanupReleases(releases.Where(r => r != null), true);
                }
                catch (Exception ex)
                {
                    _indexerStatusService.RecordFailure(Definition.Id);
                    _logger.Warn(ex, "YouTube FetchRecent failed");
                    return Array.Empty<ReleaseInfo>();
                }
            });
        }

        public override async Task<IList<ReleaseInfo>> Fetch(AlbumSearchCriteria searchCriteria)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var releases = new List<ReleaseInfo>();
                    var album = searchCriteria.Albums?.FirstOrDefault();
                    var foreignAlbumId = album?.ForeignAlbumId;
                    var artistName = searchCriteria.Artist?.Name;
                    var albumTitle = searchCriteria.AlbumTitle ?? album?.Title;

                    if (foreignAlbumId.IsNotNullOrWhiteSpace() && YouTubeIds.IsYouTubeId(foreignAlbumId))
                    {
                        if (YouTubeIds.IsUploadsAlbum(foreignAlbumId))
                        {
                            var channelId = YouTubeIds.ExtractChannelId(foreignAlbumId);
                            var uploads = _ytDlp.GetChannelUploads(channelId, Settings.MaxResults, Settings.CookiesPath, Settings.YtDlpPath);
                            releases.AddRange(MapPlaylistReleases(uploads, artistName, "Uploads"));
                        }
                        else
                        {
                            var playlist = _ytDlp.GetPlaylist(foreignAlbumId, Settings.MaxResults, Settings.CookiesPath, Settings.YtDlpPath);
                            releases.AddRange(MapPlaylistReleases(playlist, artistName, albumTitle));
                        }
                    }

                    if (releases.Count == 0)
                    {
                        var query = string.Join(" ", new[] { artistName, albumTitle }.Where(s => s.IsNotNullOrWhiteSpace()));
                        if (query.IsNotNullOrWhiteSpace())
                        {
                            var videos = _ytDlp.SearchVideos(query, Settings.MaxResults, Settings.CookiesPath, Settings.YtDlpPath);
                            releases.AddRange(videos.Select(v => MapRelease(v, artistName, albumTitle)));
                        }
                    }

                    _indexerStatusService.RecordSuccess(Definition.Id);
                    return CleanupReleases(releases.Where(r => r != null));
                }
                catch (Exception ex)
                {
                    _indexerStatusService.RecordFailure(Definition.Id);
                    _logger.Warn(ex, "YouTube album search failed");
                    return Array.Empty<ReleaseInfo>();
                }
            });
        }

        public override async Task<IList<ReleaseInfo>> Fetch(ArtistSearchCriteria searchCriteria)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var releases = new List<ReleaseInfo>();
                    var artist = searchCriteria.Artist;
                    var channelId = artist?.ForeignArtistId != null
                        ? YouTubeIds.ExtractChannelId(artist.ForeignArtistId)
                        : null;

                    if (channelId.IsNotNullOrWhiteSpace())
                    {
                        var uploads = _ytDlp.GetChannelUploads(channelId, Settings.MaxResults, Settings.CookiesPath, Settings.YtDlpPath);
                        releases.AddRange(MapPlaylistReleases(uploads, artist?.Name, "Uploads"));
                    }
                    else if (artist?.Name.IsNotNullOrWhiteSpace() == true)
                    {
                        var videos = _ytDlp.SearchVideos(artist.Name, Settings.MaxResults, Settings.CookiesPath, Settings.YtDlpPath);
                        releases.AddRange(videos.Select(v => MapRelease(v, artist.Name, null)));
                    }

                    _indexerStatusService.RecordSuccess(Definition.Id);
                    return CleanupReleases(releases.Where(r => r != null));
                }
                catch (Exception ex)
                {
                    _indexerStatusService.RecordFailure(Definition.Id);
                    _logger.Warn(ex, "YouTube artist search failed");
                    return Array.Empty<ReleaseInfo>();
                }
            });
        }

        public override HttpRequest GetDownloadRequest(string link)
        {
            return new HttpRequest(link);
        }

        protected override async Task Test(List<ValidationFailure> failures)
        {
            await Task.Run(() =>
            {
                if (!_ytDlp.TestExecutable(Settings.YtDlpPath, Settings.CookiesPath))
                {
                    failures.Add(new ValidationFailure("YtDlpPath", "Unable to run yt-dlp. Check the path and that yt-dlp is installed."));
                }
            });
        }

        private IEnumerable<ReleaseInfo> MapPlaylistReleases(YtDlpEntry playlist, string artist, string album)
        {
            if (playlist == null)
            {
                yield break;
            }

            var entries = playlist.Entries;
            if (entries == null || entries.Count == 0)
            {
                if (playlist.Id.IsNotNullOrWhiteSpace())
                {
                    yield return MapRelease(playlist, artist, album);
                }

                yield break;
            }

            foreach (var entry in entries)
            {
                var release = MapRelease(entry, artist, album);
                if (release != null)
                {
                    yield return release;
                }
            }
        }

        private ReleaseInfo MapRelease(YtDlpEntry entry, string artist = null, string album = null)
        {
            if (entry == null || entry.Id.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (entry.Type == "playlist" || entry.Type == "channel")
            {
                return null;
            }

            var videoId = entry.Id;
            var url = YouTubeIds.ToVideoUrl(videoId);
            var title = entry.Title.IsNullOrWhiteSpace() ? videoId : entry.Title;
            var channel = artist.IsNotNullOrWhiteSpace() ? artist : entry.ResolvedChannelName;
            var format = _configService.YoutubeAudioFormat.IsNullOrWhiteSpace()
                ? "MP3"
                : _configService.YoutubeAudioFormat.ToUpperInvariant();

            var releaseTitle = channel.IsNotNullOrWhiteSpace()
                ? $"{channel} - {title} [{format}]"
                : $"{title} [{format}]";

            var publishDate = DateTime.UtcNow;
            if (entry.Timestamp.HasValue)
            {
                publishDate = DateTimeOffset.FromUnixTimeSeconds(entry.Timestamp.Value).UtcDateTime;
            }

            long size = 0;
            if (entry.Duration.HasValue)
            {
                size = (long)(entry.Duration.Value * 16000);
            }

            return new ReleaseInfo
            {
                Guid = YouTubeIds.Video(videoId),
                Title = releaseTitle,
                Artist = channel,
                Album = album,
                DownloadUrl = url,
                InfoUrl = url,
                PublishDate = publishDate,
                Size = size,
                Source = "YouTube",
                Container = format,
                Codec = format
            };
        }
    }
}
