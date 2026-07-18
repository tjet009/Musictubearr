using System;
using System.IO;
using System.Threading.Tasks;
using Lidarr.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Download.YtDlp;

namespace Lidarr.Api.V1.Config
{
    [V1ApiController("config/youtube")]
    public class YouTubeConfigController : Controller
    {
        private readonly IYtDlpService _ytDlp;
        private readonly IYtDlpInstaller _ytDlpInstaller;
        private readonly IYouTubeCookiesService _cookiesService;

        public YouTubeConfigController(IYtDlpService ytDlp,
                                        IYtDlpInstaller ytDlpInstaller,
                                        IYouTubeCookiesService cookiesService)
        {
            _ytDlp = ytDlp;
            _ytDlpInstaller = ytDlpInstaller;
            _cookiesService = cookiesService;
        }

        [HttpGet("tools")]
        public object GetToolsStatus()
        {
            return _ytDlpInstaller.GetStatus();
        }

        [HttpGet("cookies/status")]
        public object GetCookiesStatus()
        {
            return _cookiesService.GetStatus();
        }

        [HttpPost("download-ytdlp")]
        public object DownloadYtDlp([FromQuery] bool force = false)
        {
            var result = _ytDlpInstaller.EnsureYtDlp(force);
            return new
            {
                isValid = result.Success,
                path = result.Path,
                message = result.Message,
                downloaded = result.Downloaded,
                ytDlpPath = result.Path
            };
        }

        [HttpPost("download-ffmpeg")]
        public object DownloadFfmpeg([FromQuery] bool force = false)
        {
            var result = _ytDlpInstaller.EnsureFfmpeg(force);
            return new
            {
                isValid = result.Success,
                path = result.Path,
                message = result.Message,
                downloaded = result.Downloaded,
                ffmpegPath = result.Path
            };
        }

        [HttpPost("cookies")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadCookies(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { isValid = false, message = "No cookies file uploaded" });
            }

            var temp = Path.Combine(Path.GetTempPath(), "musictubearr-cookies-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                await using (var stream = global::System.IO.File.OpenWrite(temp))
                {
                    await file.CopyToAsync(stream);
                }

                var result = _cookiesService.SaveUploadedFile(temp);
                if (!result.Success)
                {
                    return BadRequest(new { isValid = false, message = result.Message });
                }

                return Ok(new
                {
                    isValid = true,
                    path = result.Path,
                    youtubeCookiesPath = result.Path,
                    message = result.Message,
                    convertedFromHeader = result.ConvertedFromHeader,
                    status = _cookiesService.GetStatus()
                });
            }
            finally
            {
                try
                {
                    if (global::System.IO.File.Exists(temp))
                    {
                        global::System.IO.File.Delete(temp);
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }

        [HttpPost("cookies/paste")]
        public IActionResult PasteCookies([FromBody] YouTubeCookiesPasteRequest request)
        {
            var result = _cookiesService.SaveRaw(request?.Content);
            if (!result.Success)
            {
                return BadRequest(new { isValid = false, message = result.Message });
            }

            return Ok(new
            {
                isValid = true,
                path = result.Path,
                youtubeCookiesPath = result.Path,
                message = result.Message,
                convertedFromHeader = result.ConvertedFromHeader,
                status = _cookiesService.GetStatus()
            });
        }

        [HttpPost("cookies/import")]
        public IActionResult ImportCookies()
        {
            var result = _cookiesService.ImportFromKnownLocations();
            if (!result.Success)
            {
                return BadRequest(new { isValid = false, message = result.Message });
            }

            return Ok(new
            {
                isValid = true,
                path = result.Path,
                youtubeCookiesPath = result.Path,
                message = result.Message,
                status = _cookiesService.GetStatus()
            });
        }

        [HttpDelete("cookies")]
        public object ClearCookies()
        {
            _cookiesService.Clear();
            return new
            {
                isValid = true,
                message = "Cookies cleared",
                youtubeCookiesPath = string.Empty,
                status = _cookiesService.GetStatus()
            };
        }

        [HttpPost("test")]
        public object Test()
        {
            var status = _ytDlpInstaller.GetStatus();
            if (!status.YtDlpAvailable)
            {
                var install = _ytDlpInstaller.EnsureYtDlp();
                if (!install.Success)
                {
                    return new { isValid = false, message = install.Message };
                }
            }

            var ok = _ytDlp.TestExecutable();
            if (!ok)
            {
                return new { isValid = false, message = "yt-dlp is not available. Use Download yt-dlp in Settings." };
            }

            var cookieStatus = _cookiesService.GetStatus();
            if (!cookieStatus.FileExists)
            {
                return new
                {
                    isValid = false,
                    message = "No cookies file found. Upload cookies.txt, paste a Cookie header, or put the file in docker/cookies/cookies.txt.",
                    cookies = cookieStatus
                };
            }

            if (!cookieStatus.HasLoginCookies)
            {
                return new
                {
                    isValid = false,
                    message = cookieStatus.Message,
                    cookies = cookieStatus
                };
            }

            try
            {
                _ytDlp.SearchVideos("test", 1);
                return new
                {
                    isValid = true,
                    message = "yt-dlp + YouTube cookies are working",
                    tools = status,
                    cookies = cookieStatus
                };
            }
            catch (Exception ex)
            {
                var hint = ex.Message;
                if (hint.Contains("Sign in", StringComparison.OrdinalIgnoreCase) ||
                    hint.Contains("bot", StringComparison.OrdinalIgnoreCase) ||
                    hint.Contains("confirm", StringComparison.OrdinalIgnoreCase))
                {
                    hint += " — re-export fresh YouTube cookies while signed in (private window), then Upload or Paste again.";
                }

                return new { isValid = false, message = hint, cookies = cookieStatus };
            }
        }
    }

    public class YouTubeCookiesPasteRequest
    {
        public string Content { get; set; }
    }
}
