using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.YtDlp;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download.Clients.YtDlp
{
    public class YtDlpClient : DownloadClientBase<YtDlpSettings>
    {
        private readonly IYtDlpService _ytDlp;
        private readonly IYtDlpJobTracker _jobTracker;

        public override string Name => "yt-dlp";
        public override string Protocol => nameof(YouTubeDownloadProtocol);

        public YtDlpClient(IYtDlpService ytDlp,
                           IYtDlpJobTracker jobTracker,
                           IConfigService configService,
                           IDiskProvider diskProvider,
                           IRemotePathMappingService remotePathMappingService,
                           ILocalizationService localizationService,
                           Logger logger)
            : base(configService, diskProvider, remotePathMappingService, localizationService, logger)
        {
            _ytDlp = ytDlp;
            _jobTracker = jobTracker;
        }

        public override async Task<string> Download(RemoteAlbum remoteAlbum, IIndexer indexer)
        {
            var url = remoteAlbum.Release.DownloadUrl;
            if (url.IsNullOrWhiteSpace())
            {
                throw new DownloadClientException("Release has no YouTube download URL");
            }

            var downloadId = "ytdlp_" + Guid.NewGuid().ToString("N");
            var jobFolder = Path.Combine(Settings.DownloadFolder, downloadId);
            Directory.CreateDirectory(jobFolder);

            var job = new YtDlpJob
            {
                DownloadId = downloadId,
                Title = remoteAlbum.Release.Title,
                Url = url,
                OutputPath = jobFolder,
                Status = DownloadItemStatus.Queued,
                Started = DateTime.UtcNow,
                DefinitionId = Definition.Id,
                TotalSize = remoteAlbum.Release.Size
            };

            _jobTracker.Add(job);

            _ = Task.Run(() => RunDownload(job));

            return await Task.FromResult(downloadId);
        }

        private async Task RunDownload(YtDlpJob job)
        {
            job.Status = DownloadItemStatus.Downloading;
            _jobTracker.Update(job);

            try
            {
                var format = Settings.AudioFormat.IsNullOrWhiteSpace()
                    ? _configService.YoutubeAudioFormat
                    : Settings.AudioFormat;

                if (format.IsNullOrWhiteSpace() || format.Equals("Default", StringComparison.OrdinalIgnoreCase))
                {
                    format = "mp3";
                }

                format = YtDlpService.NormalizeAudioFormat(format);

                var quality = Settings.AudioQuality.IsNullOrWhiteSpace()
                    ? _configService.YoutubeAudioQuality
                    : Settings.AudioQuality;

                await _ytDlp.DownloadAudioAsync(
                    job.Url,
                    job.OutputPath,
                    Settings.OutputTemplate.IsNullOrWhiteSpace() ? "%(title)s.%(ext)s" : Settings.OutputTemplate,
                    format,
                    quality.IsNullOrWhiteSpace() ? "0" : quality,
                    Settings.CookiesPath,
                    Settings.YtDlpPath,
                    Settings.FfmpegPath,
                    Settings.ExtraArguments);

                job.Status = DownloadItemStatus.Completed;
                job.Completed = DateTime.UtcNow;
                job.Message = "Download completed";

                var files = _diskProvider.GetFiles(job.OutputPath, false).ToList();
                if (files.Count > 0)
                {
                    job.TotalSize = files.Sum(f => _diskProvider.GetFileSize(f));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "yt-dlp download failed for {0}", job.Title);
                job.Status = DownloadItemStatus.Failed;
                job.Message = ex.Message;
                job.Completed = DateTime.UtcNow;
            }

            _jobTracker.Update(job);
        }

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            foreach (var job in _jobTracker.GetAll().Where(j => j.DefinitionId == Definition.Id || j.DefinitionId == 0))
            {
                var item = new DownloadClientItem
                {
                    DownloadClientInfo = DownloadClientItemClientInfo.FromDownloadClient(this, false),
                    DownloadId = job.DownloadId,
                    Category = "MusicTubearr",
                    Title = job.Title,
                    TotalSize = job.TotalSize,
                    RemainingSize = job.Status == DownloadItemStatus.Completed ? 0 : job.TotalSize,
                    OutputPath = new OsPath(job.OutputPath),
                    Status = job.Status,
                    Message = job.Message,
                    CanMoveFiles = true,
                    CanBeRemoved = job.Status == DownloadItemStatus.Completed || job.Status == DownloadItemStatus.Failed
                };

                if (job.Status == DownloadItemStatus.Downloading)
                {
                    item.RemainingTime = TimeSpan.FromMinutes(5);
                }

                yield return item;
            }

            // Also surface completed folders that may have been left on disk after restart
            if (Settings.DownloadFolder.IsNotNullOrWhiteSpace() && _diskProvider.FolderExists(Settings.DownloadFolder))
            {
                foreach (var dir in _diskProvider.GetDirectories(Settings.DownloadFolder))
                {
                    var name = Path.GetFileName(dir);
                    if (!name.StartsWith("ytdlp_", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (_jobTracker.Get(name) != null)
                    {
                        continue;
                    }

                    var files = _diskProvider.GetFiles(dir, false).ToList();
                    if (files.Count == 0)
                    {
                        continue;
                    }

                    yield return new DownloadClientItem
                    {
                        DownloadClientInfo = DownloadClientItemClientInfo.FromDownloadClient(this, false),
                        DownloadId = name,
                        Category = "MusicTubearr",
                        Title = name,
                        TotalSize = files.Sum(f => _diskProvider.GetFileSize(f)),
                        RemainingSize = 0,
                        OutputPath = new OsPath(dir),
                        Status = DownloadItemStatus.Completed,
                        CanMoveFiles = true,
                        CanBeRemoved = true
                    };
                }
            }
        }

        public override void RemoveItem(DownloadClientItem item, bool deleteData)
        {
            _jobTracker.Remove(item.DownloadId);

            if (deleteData)
            {
                DeleteItemData(item);
            }
        }

        public override DownloadClientInfo GetStatus()
        {
            return new DownloadClientInfo
            {
                IsLocalhost = true,
                OutputRootFolders = new List<OsPath> { new OsPath(Settings.DownloadFolder) }
            };
        }

        public override void MarkItemAsImported(DownloadClientItem downloadClientItem)
        {
            // No remote state to update
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestFolder(Settings.DownloadFolder, "DownloadFolder"));

            if (!_ytDlp.TestExecutable(Settings.YtDlpPath, Settings.CookiesPath))
            {
                failures.Add(new ValidationFailure("YtDlpPath", "Unable to run yt-dlp. Install yt-dlp or set the correct path."));
            }
        }
    }
}
