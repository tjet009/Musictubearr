using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Processes;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Tags;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.CustomScript
{
    public class CustomScript : NotificationBase<CustomScriptSettings>
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IConfigService _configService;
        private readonly IDiskProvider _diskProvider;
        private readonly IProcessProvider _processProvider;
        private readonly ITagRepository _tagRepository;
        private readonly Logger _logger;

        public CustomScript(IConfigFileProvider configFileProvider,
            IConfigService configService,
            IDiskProvider diskProvider,
            IProcessProvider processProvider,
            ITagRepository tagRepository,
            Logger logger)
        {
            _configFileProvider = configFileProvider;
            _configService = configService;
            _diskProvider = diskProvider;
            _processProvider = processProvider;
            _tagRepository = tagRepository;
            _logger = logger;
        }

        public override string Name => "Custom Script";

        public override string Link => "https://github.com/tjet009/Musictubearr/custom-scripts";

        public override ProviderMessage Message => new ProviderMessage("Testing will execute the script with the EventType set to Test, ensure your script handles this correctly", ProviderMessageType.Warning);

        public override void OnGrab(GrabMessage message)
        {
            var artist = message.Artist;
            var remoteAlbum = message.RemoteAlbum;
            var releaseGroup = remoteAlbum.ParsedAlbumInfo.ReleaseGroup;
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("MusicTubearr_EventType", "Grab");
            environmentVariables.Add("MusicTubearr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("MusicTubearr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("MusicTubearr_Artist_Id", artist.Id.ToString());
            environmentVariables.Add("MusicTubearr_Artist_Name", artist.Metadata.Value.Name);
            environmentVariables.Add("MusicTubearr_Artist_MBId", artist.Metadata.Value.ForeignArtistId);
            environmentVariables.Add("MusicTubearr_Artist_Type", artist.Metadata.Value.Type);
            environmentVariables.Add("MusicTubearr_Artist_Genres", string.Join("|", artist.Metadata.Value.Genres));
            environmentVariables.Add("MusicTubearr_Artist_Tags", string.Join("|", GetTagLabels(artist)));
            environmentVariables.Add("MusicTubearr_Release_AlbumCount", remoteAlbum.Albums.Count.ToString());
            environmentVariables.Add("MusicTubearr_Release_AlbumReleaseDates", string.Join(",", remoteAlbum.Albums.Select(e => e.ReleaseDate)));
            environmentVariables.Add("MusicTubearr_Release_AlbumTitles", string.Join("|", remoteAlbum.Albums.Select(e => e.Title)));
            environmentVariables.Add("MusicTubearr_Release_AlbumOverviews", string.Join("|", remoteAlbum.Albums.Select(e => e.Overview)));
            environmentVariables.Add("MusicTubearr_Release_AlbumMBIds", string.Join("|", remoteAlbum.Albums.Select(e => e.ForeignAlbumId)));
            environmentVariables.Add("MusicTubearr_Release_Title", remoteAlbum.Release.Title);
            environmentVariables.Add("MusicTubearr_Release_Indexer", remoteAlbum.Release.Indexer ?? string.Empty);
            environmentVariables.Add("MusicTubearr_Release_Size", remoteAlbum.Release.Size.ToString());
            environmentVariables.Add("MusicTubearr_Release_Quality", remoteAlbum.ParsedAlbumInfo.Quality.Quality.Name);
            environmentVariables.Add("MusicTubearr_Release_QualityVersion", remoteAlbum.ParsedAlbumInfo.Quality.Revision.Version.ToString());
            environmentVariables.Add("MusicTubearr_Release_ReleaseGroup", releaseGroup ?? string.Empty);
            environmentVariables.Add("MusicTubearr_Release_IndexerFlags", remoteAlbum.Release.IndexerFlags.ToString());
            environmentVariables.Add("MusicTubearr_Download_Client", message.DownloadClientName ?? string.Empty);
            environmentVariables.Add("MusicTubearr_Download_Client_Type", message.DownloadClientType ?? string.Empty);
            environmentVariables.Add("MusicTubearr_Download_Id", message.DownloadId ?? string.Empty);
            environmentVariables.Add("MusicTubearr_Release_CustomFormat", string.Join("|", remoteAlbum.CustomFormats));
            environmentVariables.Add("MusicTubearr_Release_CustomFormatScore", remoteAlbum.CustomFormatScore.ToString());

            ExecuteScript(environmentVariables);
        }

        public override void OnReleaseImport(AlbumDownloadMessage message)
        {
            var artist = message.Artist;
            var album = message.Album;
            var release = message.Release;
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("MusicTubearr_EventType", "AlbumDownload");
            environmentVariables.Add("MusicTubearr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("MusicTubearr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("MusicTubearr_Artist_Id", artist.Id.ToString());
            environmentVariables.Add("MusicTubearr_Artist_Name", artist.Metadata.Value.Name);
            environmentVariables.Add("MusicTubearr_Artist_Path", artist.Path);
            environmentVariables.Add("MusicTubearr_Artist_MBId", artist.Metadata.Value.ForeignArtistId);
            environmentVariables.Add("MusicTubearr_Artist_Type", artist.Metadata.Value.Type);
            environmentVariables.Add("MusicTubearr_Artist_Genres", string.Join("|", artist.Metadata.Value.Genres));
            environmentVariables.Add("MusicTubearr_Artist_Tags", string.Join("|", GetTagLabels(artist)));
            environmentVariables.Add("MusicTubearr_Album_Id", album.Id.ToString());
            environmentVariables.Add("MusicTubearr_Album_Title", album.Title);
            environmentVariables.Add("MusicTubearr_Album_Overview", album.Overview);
            environmentVariables.Add("MusicTubearr_Album_MBId", album.ForeignAlbumId);
            environmentVariables.Add("MusicTubearr_AlbumRelease_MBId", release.ForeignReleaseId);
            environmentVariables.Add("MusicTubearr_Album_ReleaseDate", album.ReleaseDate.ToString());
            environmentVariables.Add("MusicTubearr_Download_Client", message.DownloadClientInfo?.Name ?? string.Empty);
            environmentVariables.Add("MusicTubearr_Download_Client_Type", message.DownloadClientInfo?.Type ?? string.Empty);
            environmentVariables.Add("MusicTubearr_Download_Id", message.DownloadId ?? string.Empty);

            if (message.TrackFiles.Any())
            {
                environmentVariables.Add("MusicTubearr_AddedTrackPaths", string.Join("|", message.TrackFiles.Select(e => e.Path)));
            }

            if (message.OldFiles.Any())
            {
                environmentVariables.Add("MusicTubearr_DeletedPaths", string.Join("|", message.OldFiles.Select(e => e.Path)));
                environmentVariables.Add("MusicTubearr_DeletedDateAdded", string.Join("|", message.OldFiles.Select(e => e.DateAdded)));
            }

            ExecuteScript(environmentVariables);
        }

        public override void OnRename(Artist artist, List<RenamedTrackFile> renamedFiles)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("MusicTubearr_EventType", "Rename");
            environmentVariables.Add("MusicTubearr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("MusicTubearr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("MusicTubearr_Artist_Id", artist.Id.ToString());
            environmentVariables.Add("MusicTubearr_Artist_Name", artist.Metadata.Value.Name);
            environmentVariables.Add("MusicTubearr_Artist_Path", artist.Path);
            environmentVariables.Add("MusicTubearr_Artist_MBId", artist.Metadata.Value.ForeignArtistId);
            environmentVariables.Add("MusicTubearr_Artist_Type", artist.Metadata.Value.Type);
            environmentVariables.Add("MusicTubearr_Artist_Genres", string.Join("|", artist.Metadata.Value.Genres));
            environmentVariables.Add("MusicTubearr_Artist_Tags", string.Join("|", GetTagLabels(artist)));
            environmentVariables.Add("MusicTubearr_TrackFile_Ids", string.Join(",", renamedFiles.Select(e => e.TrackFile.Id)));
            environmentVariables.Add("MusicTubearr_TrackFile_Paths", string.Join("|", renamedFiles.Select(e => e.TrackFile.Path)));
            environmentVariables.Add("MusicTubearr_TrackFile_PreviousPaths", string.Join("|", renamedFiles.Select(e => e.PreviousPath)));

            ExecuteScript(environmentVariables);
        }

        public override void OnTrackRetag(TrackRetagMessage message)
        {
            var artist = message.Artist;
            var album = message.Album;
            var release = message.Release;
            var trackFile = message.TrackFile;
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("MusicTubearr_EventType", "TrackRetag");
            environmentVariables.Add("MusicTubearr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("MusicTubearr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("MusicTubearr_Artist_Id", artist.Id.ToString());
            environmentVariables.Add("MusicTubearr_Artist_Name", artist.Metadata.Value.Name);
            environmentVariables.Add("MusicTubearr_Artist_Path", artist.Path);
            environmentVariables.Add("MusicTubearr_Artist_MBId", artist.Metadata.Value.ForeignArtistId);
            environmentVariables.Add("MusicTubearr_Artist_Type", artist.Metadata.Value.Type);
            environmentVariables.Add("MusicTubearr_Artist_Genres", string.Join("|", artist.Metadata.Value.Genres));
            environmentVariables.Add("MusicTubearr_Artist_Tags", string.Join("|", GetTagLabels(artist)));
            environmentVariables.Add("MusicTubearr_Album_Id", album.Id.ToString());
            environmentVariables.Add("MusicTubearr_Album_Title", album.Title);
            environmentVariables.Add("MusicTubearr_Album_Overview", album.Overview);
            environmentVariables.Add("MusicTubearr_Album_MBId", album.ForeignAlbumId);
            environmentVariables.Add("MusicTubearr_AlbumRelease_MBId", release.ForeignReleaseId);
            environmentVariables.Add("MusicTubearr_Album_ReleaseDate", album.ReleaseDate.ToString());
            environmentVariables.Add("MusicTubearr_TrackFile_Id", trackFile.Id.ToString());
            environmentVariables.Add("MusicTubearr_TrackFile_TrackCount", trackFile.Tracks.Value.Count.ToString());
            environmentVariables.Add("MusicTubearr_TrackFile_Path", trackFile.Path);
            environmentVariables.Add("MusicTubearr_TrackFile_TrackIds", string.Join(",", trackFile.Tracks.Value.Select(e => e.Id)));
            environmentVariables.Add("MusicTubearr_TrackFile_TrackNumbers", string.Join(",", trackFile.Tracks.Value.Select(e => e.TrackNumber)));
            environmentVariables.Add("MusicTubearr_TrackFile_TrackTitles", string.Join("|", trackFile.Tracks.Value.Select(e => e.Title)));
            environmentVariables.Add("MusicTubearr_TrackFile_Quality", trackFile.Quality.Quality.Name);
            environmentVariables.Add("MusicTubearr_TrackFile_QualityVersion", trackFile.Quality.Revision.Version.ToString());
            environmentVariables.Add("MusicTubearr_TrackFile_ReleaseGroup", trackFile.ReleaseGroup ?? string.Empty);
            environmentVariables.Add("MusicTubearr_TrackFile_SceneName", trackFile.SceneName ?? string.Empty);
            environmentVariables.Add("MusicTubearr_Tags_Diff", message.Diff.ToJson());
            environmentVariables.Add("MusicTubearr_Tags_Scrubbed", message.Scrubbed.ToString());

            ExecuteScript(environmentVariables);
        }

        public override void OnArtistAdd(ArtistAddMessage message)
        {
            var artist = message.Artist;
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("MusicTubearr_EventType", "ArtistAdd");
            environmentVariables.Add("MusicTubearr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("MusicTubearr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("MusicTubearr_Artist_Id", artist.Id.ToString());
            environmentVariables.Add("MusicTubearr_Artist_Title", artist.Metadata.Value.Name);
            environmentVariables.Add("MusicTubearr_Artist_Path", artist.Path);
            environmentVariables.Add("MusicTubearr_Artist_MBId", artist.Metadata.Value.ForeignArtistId.ToString());
            environmentVariables.Add("MusicTubearr_Artist_Type", artist.Metadata.Value.Type);
            environmentVariables.Add("MusicTubearr_Artist_Genres", string.Join("|", artist.Metadata.Value.Genres));
            environmentVariables.Add("MusicTubearr_Artist_Tags", string.Join("|", GetTagLabels(artist)));

            ExecuteScript(environmentVariables);
        }

        public override void OnArtistDelete(ArtistDeleteMessage deleteMessage)
        {
            var artist = deleteMessage.Artist;
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("MusicTubearr_EventType", "ArtistDeleted");
            environmentVariables.Add("MusicTubearr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("MusicTubearr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("MusicTubearr_Artist_Id", artist.Id.ToString());
            environmentVariables.Add("MusicTubearr_Artist_Title", artist.Metadata.Value.Name);
            environmentVariables.Add("MusicTubearr_Artist_Path", artist.Path);
            environmentVariables.Add("MusicTubearr_Artist_MBId", artist.Metadata.Value.ForeignArtistId.ToString());
            environmentVariables.Add("MusicTubearr_Artist_Type", artist.Metadata.Value.Type);
            environmentVariables.Add("MusicTubearr_Artist_Genres", string.Join("|", artist.Metadata.Value.Genres));
            environmentVariables.Add("MusicTubearr_Artist_Tags", string.Join("|", GetTagLabels(artist)));
            environmentVariables.Add("MusicTubearr_Artist_DeletedFiles", deleteMessage.DeletedFiles.ToString());

            ExecuteScript(environmentVariables);
        }

        public override void OnAlbumDelete(AlbumDeleteMessage deleteMessage)
        {
            var artist = deleteMessage.Album.Artist.Value;
            var album = deleteMessage.Album;
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("MusicTubearr_EventType", "AlbumDeleted");
            environmentVariables.Add("MusicTubearr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("MusicTubearr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("MusicTubearr_Artist_Id", artist.Id.ToString());
            environmentVariables.Add("MusicTubearr_Artist_Name", artist.Metadata.Value.Name);
            environmentVariables.Add("MusicTubearr_Artist_Path", artist.Path);
            environmentVariables.Add("MusicTubearr_Artist_MBId", artist.Metadata.Value.ForeignArtistId);
            environmentVariables.Add("MusicTubearr_Artist_Type", artist.Metadata.Value.Type);
            environmentVariables.Add("MusicTubearr_Artist_Genres", string.Join("|", artist.Metadata.Value.Genres));
            environmentVariables.Add("MusicTubearr_Artist_Tags", string.Join("|", GetTagLabels(artist)));
            environmentVariables.Add("MusicTubearr_Album_Id", album.Id.ToString());
            environmentVariables.Add("MusicTubearr_Album_Title", album.Title);
            environmentVariables.Add("MusicTubearr_Album_Overview", album.Overview);
            environmentVariables.Add("MusicTubearr_Album_MBId", album.ForeignAlbumId);
            environmentVariables.Add("MusicTubearr_Album_ReleaseDate", album.ReleaseDate.ToString());
            environmentVariables.Add("MusicTubearr_Artist_DeletedFiles", deleteMessage.DeletedFiles.ToString());

            ExecuteScript(environmentVariables);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("MusicTubearr_EventType", "HealthIssue");
            environmentVariables.Add("MusicTubearr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("MusicTubearr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("MusicTubearr_Health_Issue_Level", Enum.GetName(typeof(HealthCheckResult), healthCheck.Type));
            environmentVariables.Add("MusicTubearr_Health_Issue_Message", healthCheck.Message);
            environmentVariables.Add("MusicTubearr_Health_Issue_Type", healthCheck.Source.Name);
            environmentVariables.Add("MusicTubearr_Health_Issue_Wiki", healthCheck.WikiUrl.ToString() ?? string.Empty);

            ExecuteScript(environmentVariables);
        }

        public override void OnHealthRestored(HealthCheck.HealthCheck previousCheck)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("MusicTubearr_EventType", "HealthRestored");
            environmentVariables.Add("MusicTubearr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("MusicTubearr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("MusicTubearr_Health_Restored_Level", Enum.GetName(typeof(HealthCheckResult), previousCheck.Type));
            environmentVariables.Add("MusicTubearr_Health_Restored_Message", previousCheck.Message);
            environmentVariables.Add("MusicTubearr_Health_Restored_Type", previousCheck.Source.Name);
            environmentVariables.Add("MusicTubearr_Health_Restored_Wiki", previousCheck.WikiUrl.ToString() ?? string.Empty);

            ExecuteScript(environmentVariables);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("MusicTubearr_EventType", "ApplicationUpdate");
            environmentVariables.Add("MusicTubearr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("MusicTubearr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("MusicTubearr_Update_Message", updateMessage.Message);
            environmentVariables.Add("MusicTubearr_Update_NewVersion", updateMessage.NewVersion.ToString());
            environmentVariables.Add("MusicTubearr_Update_PreviousVersion", updateMessage.PreviousVersion.ToString());

            ExecuteScript(environmentVariables);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            if (!_diskProvider.FileExists(Settings.Path))
            {
                failures.Add(new NzbDroneValidationFailure("Path", "File does not exist"));
            }

            if (failures.Empty())
            {
                try
                {
                    var environmentVariables = new StringDictionary();
                    environmentVariables.Add("MusicTubearr_EventType", "Test");
                    environmentVariables.Add("MusicTubearr_InstanceName", _configFileProvider.InstanceName);
                    environmentVariables.Add("MusicTubearr_ApplicationUrl", _configService.ApplicationUrl);

                    var processOutput = ExecuteScript(environmentVariables);

                    if (processOutput.ExitCode != 0)
                    {
                        failures.Add(new NzbDroneValidationFailure(string.Empty, $"Script exited with code: {processOutput.ExitCode}"));
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                    failures.Add(new NzbDroneValidationFailure(string.Empty, ex.Message));
                }
            }

            return new ValidationResult(failures);
        }

        private ProcessOutput ExecuteScript(StringDictionary environmentVariables)
        {
            _logger.Debug("Executing external script: {0}", Settings.Path);

            var processOutput = _processProvider.StartAndCapture(Settings.Path, Settings.Arguments, environmentVariables);

            _logger.Debug("Executed external script: {0} - Status: {1}", Settings.Path, processOutput.ExitCode);
            _logger.Debug($"Script Output: {System.Environment.NewLine}{string.Join(System.Environment.NewLine, processOutput.Lines)}");

            return processOutput;
        }

        private List<string> GetTagLabels(Artist artist)
        {
            if (artist == null)
            {
                return new List<string>();
            }

            return _tagRepository.GetTags(artist.Tags)
                .Select(s => s.Label)
                .Where(l => l.IsNotNullOrWhiteSpace())
                .OrderBy(l => l)
                .ToList();
        }
    }
}
