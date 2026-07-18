namespace NzbDrone.Core.Download.YtDlp
{
    public interface IYtDlpInstaller
    {
        YtDlpInstallResult EnsureYtDlp(bool force = false);
        YtDlpInstallResult EnsureFfmpeg(bool force = false);
        YtDlpToolsStatus GetStatus();
    }

    public class YtDlpInstallResult
    {
        public bool Success { get; set; }
        public string Path { get; set; }
        public string Message { get; set; }
        public bool Downloaded { get; set; }
    }

    public class YtDlpToolsStatus
    {
        public bool YtDlpAvailable { get; set; }
        public string YtDlpPath { get; set; }
        public string YtDlpVersion { get; set; }
        public bool FfmpegAvailable { get; set; }
        public string FfmpegPath { get; set; }
    }
}
