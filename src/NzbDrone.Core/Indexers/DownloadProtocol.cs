namespace NzbDrone.Core.Indexers
{
    public interface IDownloadProtocol
    {
    }

    public class UsenetDownloadProtocol : IDownloadProtocol
    {
    }

    public class TorrentDownloadProtocol : IDownloadProtocol
    {
    }

    public class YouTubeDownloadProtocol : IDownloadProtocol
    {
    }
}
