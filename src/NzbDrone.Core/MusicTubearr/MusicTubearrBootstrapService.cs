using System;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients.YtDlp;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.YouTube;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MusicTubearr
{
    public class MusicTubearrBootstrapService : IHandle<ApplicationStartedEvent>
    {
        private readonly IIndexerFactory _indexerFactory;
        private readonly IDownloadClientFactory _downloadClientFactory;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public MusicTubearrBootstrapService(IIndexerFactory indexerFactory,
                                            IDownloadClientFactory downloadClientFactory,
                                            IAppFolderInfo appFolderInfo,
                                            IDiskProvider diskProvider,
                                            Logger logger)
        {
            _indexerFactory = indexerFactory;
            _downloadClientFactory = downloadClientFactory;
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public void Handle(ApplicationStartedEvent message)
        {
            var client = EnsureYtDlpClient();
            EnsureYouTubeIndexer(client?.Id ?? 0);
        }

        private void EnsureYouTubeIndexer(int downloadClientId)
        {
            try
            {
                var existing = _indexerFactory.All().FirstOrDefault(i => i.Implementation == nameof(YouTube));
                if (existing != null)
                {
                    if (downloadClientId > 0 && existing.DownloadClientId == 0)
                    {
                        existing.DownloadClientId = downloadClientId;
                        _indexerFactory.Update(existing);
                    }

                    return;
                }

                var definition = new IndexerDefinition
                {
                    Name = "YouTube",
                    EnableRss = true,
                    EnableAutomaticSearch = true,
                    EnableInteractiveSearch = true,
                    Implementation = nameof(YouTube),
                    ConfigContract = nameof(YouTubeSettings),
                    Protocol = nameof(YouTubeDownloadProtocol),
                    DownloadClientId = downloadClientId,
                    Settings = new YouTubeSettings()
                };

                _indexerFactory.Create(definition);
                _logger.Info("Created default YouTube indexer");
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to create default YouTube indexer");
            }
        }

        private DownloadClientDefinition EnsureYtDlpClient()
        {
            try
            {
                var existing = _downloadClientFactory.All().FirstOrDefault(c => c.Implementation == nameof(YtDlpClient));
                if (existing != null)
                {
                    return existing;
                }

                var downloadFolder = Path.Combine(_appFolderInfo.AppDataFolder, "ytdlp");
                _diskProvider.CreateFolder(downloadFolder);

                var definition = new DownloadClientDefinition
                {
                    Name = "yt-dlp",
                    Enable = true,
                    Implementation = nameof(YtDlpClient),
                    ConfigContract = nameof(YtDlpSettings),
                    Protocol = nameof(YouTubeDownloadProtocol),
                    Settings = new YtDlpSettings
                    {
                        DownloadFolder = downloadFolder
                    }
                };

                var created = _downloadClientFactory.Create(definition);
                _logger.Info("Created default yt-dlp download client at {0}", downloadFolder);
                return created;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to create default yt-dlp download client");
                return null;
            }
        }
    }
}
