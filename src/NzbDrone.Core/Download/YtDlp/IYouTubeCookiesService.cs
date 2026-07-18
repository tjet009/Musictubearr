namespace NzbDrone.Core.Download.YtDlp
{
    public interface IYouTubeCookiesService
    {
        string DefaultCookiesPath { get; }
        YouTubeCookiesStatus GetStatus();
        YouTubeCookiesSaveResult SaveRaw(string content);
        YouTubeCookiesSaveResult SaveUploadedFile(string tempFilePath);
        YouTubeCookiesSaveResult ImportFromKnownLocations();
        bool Clear();
    }

    public class YouTubeCookiesStatus
    {
        public bool Configured { get; set; }
        public bool FileExists { get; set; }
        public string Path { get; set; }
        public long? ByteCount { get; set; }
        public string Message { get; set; }
        public bool LooksValid { get; set; }
        public bool HasLoginCookies { get; set; }
    }

    public class YouTubeCookiesSaveResult
    {
        public bool Success { get; set; }
        public string Path { get; set; }
        public string Message { get; set; }
        public bool ConvertedFromHeader { get; set; }
    }
}
