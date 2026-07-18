using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Download.YtDlp
{
    public class YtDlpInstaller : IYtDlpInstaller
    {
        private const string YtDlpReleaseBase = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/";

        // Windows ffmpeg essentials build (static). Linux/mac rely on PATH or Docker image.
        private const string FfmpegWindowsZip =
            "https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";

        private readonly IHttpClient _httpClient;
        private readonly IConfigService _configService;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;
        private readonly IYtDlpService _ytDlpService;
        private readonly Logger _logger;

        public YtDlpInstaller(IHttpClient httpClient,
                              IConfigService configService,
                              IAppFolderInfo appFolderInfo,
                              IDiskProvider diskProvider,
                              IYtDlpService ytDlpService,
                              Logger logger)
        {
            _httpClient = httpClient;
            _configService = configService;
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
            _ytDlpService = ytDlpService;
            _logger = logger;
        }

        public YtDlpToolsStatus GetStatus()
        {
            var ytPath = ResolveExistingYtDlp();
            var ffPath = ResolveExistingFfmpeg();

            return new YtDlpToolsStatus
            {
                YtDlpAvailable = ytPath.IsNotNullOrWhiteSpace() && IsUsableFilePath(ytPath),
                YtDlpPath = ytPath,
                YtDlpVersion = GetVersion(ytPath, "--version"),
                FfmpegAvailable = ffPath.IsNotNullOrWhiteSpace() && IsUsableToolPath(ffPath, allowDirectory: true),
                FfmpegPath = ffPath
            };
        }

        public YtDlpInstallResult EnsureYtDlp(bool force = false)
        {
            var toolsDir = GetToolsDirectory();
            _diskProvider.CreateFolder(toolsDir);

            var target = Path.Combine(toolsDir, OsInfo.IsWindows ? "yt-dlp.exe" : "yt-dlp");

            if (!force && _diskProvider.FileExists(target))
            {
                _configService.YtDlpPath = target;
                return new YtDlpInstallResult
                {
                    Success = true,
                    Path = target,
                    Message = "yt-dlp already installed",
                    Downloaded = false
                };
            }

            // Prefer PATH if present and not forcing
            if (!force)
            {
                var onPath = FindOnPath(OsInfo.IsWindows ? "yt-dlp.exe" : "yt-dlp");
                if (onPath.IsNotNullOrWhiteSpace())
                {
                    _configService.YtDlpPath = onPath;
                    return new YtDlpInstallResult
                    {
                        Success = true,
                        Path = onPath,
                        Message = "Using yt-dlp from PATH",
                        Downloaded = false
                    };
                }
            }

            try
            {
                var url = GetYtDlpDownloadUrl();
                _logger.Info("Downloading yt-dlp from {0} to {1}", url, target);
                _httpClient.DownloadFile(url, target);

                if (OsInfo.IsOsx || OsInfo.IsLinux)
                {
                    _diskProvider.SetFilePermissions(target, "755", null);
                }

                _configService.YtDlpPath = target;

                var version = GetVersion(target, "--version");
                return new YtDlpInstallResult
                {
                    Success = true,
                    Path = target,
                    Message = version.IsNullOrWhiteSpace() ? "yt-dlp downloaded" : $"yt-dlp downloaded ({version.Trim()})",
                    Downloaded = true
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to download yt-dlp");
                return new YtDlpInstallResult
                {
                    Success = false,
                    Message = "Failed to download yt-dlp: " + ex.Message
                };
            }
        }

        public YtDlpInstallResult EnsureFfmpeg(bool force = false)
        {
            var existing = ResolveExistingFfmpeg();
            if (!force && existing.IsNotNullOrWhiteSpace() && !IsBareCommandName(existing))
            {
                _configService.FfmpegPath = existing;
                return new YtDlpInstallResult
                {
                    Success = true,
                    Path = existing,
                    Message = "ffmpeg already available",
                    Downloaded = false
                };
            }

            if (!OsInfo.IsWindows)
            {
                // In Docker/Linux images ffmpeg is packaged. Native Linux/mac: ask user or rely on package manager.
                var onPath = FindOnPath("ffmpeg");
                if (onPath.IsNotNullOrWhiteSpace())
                {
                    _configService.FfmpegPath = onPath;
                    return new YtDlpInstallResult
                    {
                        Success = true,
                        Path = onPath,
                        Message = "Using ffmpeg from PATH",
                        Downloaded = false
                    };
                }

                return new YtDlpInstallResult
                {
                    Success = false,
                    Message = "ffmpeg is not installed. Use the Docker image (includes ffmpeg) or install ffmpeg via your package manager."
                };
            }

            try
            {
                var toolsDir = GetToolsDirectory();
                var ffmpegDir = Path.Combine(toolsDir, "ffmpeg");
                _diskProvider.CreateFolder(ffmpegDir);

                var zipPath = Path.Combine(toolsDir, "ffmpeg.zip");
                _logger.Info("Downloading ffmpeg from {0}", FfmpegWindowsZip);
                _httpClient.DownloadFile(FfmpegWindowsZip, zipPath);

                if (_diskProvider.FolderExists(ffmpegDir))
                {
                    try
                    {
                        _diskProvider.DeleteFolder(ffmpegDir, true);
                    }
                    catch
                    {
                        // ignore
                    }
                }

                _diskProvider.CreateFolder(ffmpegDir);
                ZipFile.ExtractToDirectory(zipPath, ffmpegDir, true);

                try
                {
                    _diskProvider.DeleteFile(zipPath);
                }
                catch
                {
                    // ignore
                }

                var exe = Directory.EnumerateFiles(ffmpegDir, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (exe.IsNullOrWhiteSpace())
                {
                    return new YtDlpInstallResult
                    {
                        Success = false,
                        Message = "ffmpeg.zip downloaded but ffmpeg.exe was not found inside the archive"
                    };
                }

                // Point at the bin directory so yt-dlp --ffmpeg-location works
                var binDir = Path.GetDirectoryName(exe);
                _configService.FfmpegPath = binDir;

                return new YtDlpInstallResult
                {
                    Success = true,
                    Path = binDir,
                    Message = "ffmpeg downloaded",
                    Downloaded = true
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to download ffmpeg");
                return new YtDlpInstallResult
                {
                    Success = false,
                    Message = "Failed to download ffmpeg: " + ex.Message
                };
            }
        }

        private string GetToolsDirectory()
        {
            return Path.Combine(_appFolderInfo.AppDataFolder, "tools");
        }

        private string ResolveExistingYtDlp()
        {
            var configured = NormalizeConfiguredPath(_configService.YtDlpPath);
            if (configured.IsNotNullOrWhiteSpace() && IsUsableFilePath(configured))
            {
                return configured;
            }

            var bundled = Path.Combine(GetToolsDirectory(), OsInfo.IsWindows ? "yt-dlp.exe" : "yt-dlp");
            if (IsUsableFilePath(bundled))
            {
                return bundled;
            }

            return FindOnPath(OsInfo.IsWindows ? "yt-dlp.exe" : "yt-dlp");
        }

        private string ResolveExistingFfmpeg()
        {
            var configured = NormalizeConfiguredPath(_configService.FfmpegPath);
            if (configured.IsNotNullOrWhiteSpace() && IsUsableToolPath(configured, allowDirectory: true))
            {
                // Prefer absolute PATH resolution over a bare command name like "ffmpeg".
                if (IsBareCommandName(configured))
                {
                    return FindOnPath(configured) ?? configured;
                }

                return configured;
            }

            var toolsFfmpeg = Path.Combine(GetToolsDirectory(), "ffmpeg");
            if (Directory.Exists(toolsFfmpeg))
            {
                var exe = Directory.EnumerateFiles(toolsFfmpeg, OsInfo.IsWindows ? "ffmpeg.exe" : "ffmpeg", SearchOption.AllDirectories).FirstOrDefault();
                if (exe.IsNotNullOrWhiteSpace())
                {
                    return Path.GetDirectoryName(exe);
                }
            }

            return FindOnPath("ffmpeg");
        }

        private static string NormalizeConfiguredPath(string path)
        {
            if (path.IsNullOrWhiteSpace())
            {
                return null;
            }

            var trimmed = path.Trim().Trim('"');
            if (IsBareCommandName(trimmed))
            {
                return FindOnPath(trimmed) ?? trimmed;
            }

            return trimmed;
        }

        private static bool IsBareCommandName(string path)
        {
            if (path.IsNullOrWhiteSpace())
            {
                return false;
            }

            return !Path.IsPathRooted(path) &&
                   path.IndexOf(Path.DirectorySeparatorChar) < 0 &&
                   path.IndexOf(Path.AltDirectorySeparatorChar) < 0;
        }

        private bool IsUsableFilePath(string path)
        {
            if (path.IsNullOrWhiteSpace())
            {
                return false;
            }

            if (IsBareCommandName(path))
            {
                return FindOnPath(path) != null;
            }

            try
            {
                return _diskProvider.FileExists(path) || File.Exists(path);
            }
            catch (ArgumentException)
            {
                return File.Exists(path);
            }
        }

        private bool IsUsableToolPath(string path, bool allowDirectory)
        {
            if (path.IsNullOrWhiteSpace())
            {
                return false;
            }

            if (IsBareCommandName(path))
            {
                return FindOnPath(path) != null;
            }

            try
            {
                if (_diskProvider.FileExists(path) || File.Exists(path))
                {
                    return true;
                }

                if (allowDirectory && (_diskProvider.FolderExists(path) || Directory.Exists(path)))
                {
                    return true;
                }
            }
            catch (ArgumentException)
            {
                return File.Exists(path) || (allowDirectory && Directory.Exists(path));
            }

            return false;
        }

        private static string GetYtDlpDownloadUrl()
        {
            if (OsInfo.IsWindows)
            {
                return YtDlpReleaseBase + "yt-dlp.exe";
            }

            if (OsInfo.IsOsx)
            {
                return RuntimeInformation.OSArchitecture == Architecture.Arm64
                    ? YtDlpReleaseBase + "yt-dlp_macos"
                    : YtDlpReleaseBase + "yt-dlp_macos";
            }

            // Linux
            return RuntimeInformation.OSArchitecture == Architecture.Arm64
                ? YtDlpReleaseBase + "yt-dlp_linux_aarch64"
                : YtDlpReleaseBase + "yt-dlp_linux";
        }

        private static string FindOnPath(string fileName)
        {
            try
            {
                var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    var candidate = Path.Combine(dir.Trim('"'), fileName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private static bool CommandExists(string pathOrName)
        {
            if (pathOrName.IsNullOrWhiteSpace())
            {
                return false;
            }

            if (File.Exists(pathOrName) || Directory.Exists(pathOrName))
            {
                return true;
            }

            return FindOnPath(pathOrName) != null;
        }

        private string GetVersion(string executable, string arg)
        {
            if (executable.IsNullOrWhiteSpace() || !IsUsableFilePath(executable))
            {
                return null;
            }

            var fileName = IsBareCommandName(executable)
                ? FindOnPath(executable) ?? executable
                : executable;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arg,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    return null;
                }

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(10000);
                return output;
            }
            catch
            {
                return null;
            }
        }
    }
}
