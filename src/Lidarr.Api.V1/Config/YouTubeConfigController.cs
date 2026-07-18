using System;
using System.IO;
using System.Threading.Tasks;
using Lidarr.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.YtDlp;

namespace Lidarr.Api.V1.Config
{
    [V1ApiController("config/youtube")]
    public class YouTubeConfigController : Controller
    {
        private readonly IConfigService _configService;
        private readonly IYtDlpService _ytDlp;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public YouTubeConfigController(IConfigService configService,
                                        IYtDlpService ytDlp,
                                        IAppFolderInfo appFolderInfo,
                                        IDiskProvider diskProvider,
                                        Logger logger)
        {
            _configService = configService;
            _ytDlp = ytDlp;
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        [HttpPost("cookies")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadCookies(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No cookies file uploaded");
            }

            var cookiesDir = Path.Combine(_appFolderInfo.AppDataFolder, "youtube");
            _diskProvider.CreateFolder(cookiesDir);

            var target = Path.Combine(cookiesDir, "cookies.txt");
            await using (var stream = _diskProvider.OpenWriteStream(target))
            {
                await file.CopyToAsync(stream);
            }

            _configService.YoutubeCookiesPath = target;
            _logger.Info("Uploaded YouTube cookies to {0}", target);

            return Ok(new { path = target });
        }

        [HttpPost("test")]
        public object Test()
        {
            var ok = _ytDlp.TestExecutable();
            if (!ok)
            {
                return new { isValid = false, message = "yt-dlp is not available. Check YtDlpPath." };
            }

            var cookies = _configService.YoutubeCookiesPath;
            if (cookies.IsNotNullOrWhiteSpace() && !_diskProvider.FileExists(cookies))
            {
                return new { isValid = false, message = $"Cookies file not found: {cookies}" };
            }

            try
            {
                _ytDlp.SearchVideos("test", 1);
                return new { isValid = true, message = "yt-dlp is working" };
            }
            catch (Exception ex)
            {
                return new { isValid = false, message = ex.Message };
            }
        }
    }
}
