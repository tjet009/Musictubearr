using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NzbDrone.Core.Download.YtDlp
{
    public interface IYtDlpService
    {
        string ResolveYtDlpPath(string overridePath = null);
        string ResolveFfmpegPath(string overridePath = null);
        string ResolveCookiesPath(string overridePath = null);

        YtDlpEntry GetJson(string urlOrQuery, int? playlistEnd = null, string cookiesPath = null, string ytDlpPath = null);
        List<YtDlpEntry> SearchVideos(string query, int maxResults = 20, string cookiesPath = null, string ytDlpPath = null);
        List<YtDlpEntry> SearchChannels(string query, int maxResults = 10, string cookiesPath = null, string ytDlpPath = null);
        YtDlpEntry GetChannel(string channelIdOrUrl, string cookiesPath = null, string ytDlpPath = null);
        YtDlpEntry GetChannelUploads(string channelId, int? playlistEnd = 100, string cookiesPath = null, string ytDlpPath = null);
        List<YtDlpEntry> GetChannelPlaylists(string channelId, string cookiesPath = null, string ytDlpPath = null);
        YtDlpEntry GetPlaylist(string playlistIdOrUrl, int? playlistEnd = null, string cookiesPath = null, string ytDlpPath = null);
        YtDlpEntry GetVideo(string videoIdOrUrl, string cookiesPath = null, string ytDlpPath = null);

        Task DownloadAudioAsync(
            string url,
            string outputDirectory,
            string outputTemplate,
            string audioFormat,
            string audioQuality,
            string cookiesPath = null,
            string ytDlpPath = null,
            string ffmpegPath = null,
            string extraArgs = null,
            CancellationToken cancellationToken = default);

        bool TestExecutable(string ytDlpPath = null, string cookiesPath = null);
    }
}
