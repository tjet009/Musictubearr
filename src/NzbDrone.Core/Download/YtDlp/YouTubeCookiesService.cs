using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Download.YtDlp
{
    public class YouTubeCookiesService : IYouTubeCookiesService
    {
        private static readonly string[] LoginCookieNames =
        {
            "LOGIN_INFO",
            "SID",
            "__Secure-1PSID",
            "__Secure-3PSID",
            "SAPISID",
            "__Secure-1PAPISID",
            "HSID",
            "SSID"
        };

        private static readonly string[] CandidatePaths =
        {
            "/cookies/cookies.txt",
            "/cookies/youtube.txt",
            "/config/youtube/cookies.txt"
        };

        private readonly IConfigService _configService;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public YouTubeCookiesService(IConfigService configService,
                                     IAppFolderInfo appFolderInfo,
                                     IDiskProvider diskProvider,
                                     Logger logger)
        {
            _configService = configService;
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public string DefaultCookiesPath => Path.Combine(_appFolderInfo.AppDataFolder, "youtube", "cookies.txt");

        public YouTubeCookiesStatus GetStatus()
        {
            var path = _configService.YoutubeCookiesPath;
            if (path.IsNullOrWhiteSpace())
            {
                path = DefaultCookiesPath;
            }

            var exists = path.IsNotNullOrWhiteSpace() && _diskProvider.FileExists(path);
            string content = null;
            if (exists)
            {
                try
                {
                    content = File.ReadAllText(path);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to read cookies file {0}", path);
                }
            }

            var looksValid = content.IsNotNullOrWhiteSpace() && LooksLikeCookies(content);
            var hasLogin = content.IsNotNullOrWhiteSpace() && HasLoginCookies(content);
            var configured = _configService.YoutubeCookiesPath.IsNotNullOrWhiteSpace();

            string message;
            if (!configured && !exists)
            {
                message = "No cookies configured. Upload a Netscape cookies.txt, paste a Cookie header, or mount docker/cookies/cookies.txt.";
            }
            else if (!exists)
            {
                message = $"Cookies path is set but the file is missing: {path}";
            }
            else if (!looksValid)
            {
                message = "Cookies file exists but does not look like Netscape cookies or a Cookie header.";
            }
            else if (!hasLogin)
            {
                message = "Cookies file loaded, but YouTube login cookies were not found. Export while signed in to YouTube.";
            }
            else
            {
                message = "YouTube cookies look ready.";
            }

            return new YouTubeCookiesStatus
            {
                Configured = configured || exists,
                FileExists = exists,
                Path = path,
                ByteCount = exists ? _diskProvider.GetFileSize(path) : null,
                LooksValid = looksValid,
                HasLoginCookies = hasLogin,
                Message = message
            };
        }

        public YouTubeCookiesSaveResult SaveRaw(string content)
        {
            if (content.IsNullOrWhiteSpace())
            {
                return Fail("Paste is empty. Paste a Netscape cookies.txt or a browser Cookie header.");
            }

            content = content.Trim().TrimStart('\uFEFF');

            string netscape;
            var converted = false;

            if (LooksLikeNetscape(content))
            {
                netscape = EnsureNetscapeHeader(content);
            }
            else if (LooksLikeCookieHeader(content))
            {
                netscape = CookieHeaderToNetscape(content);
                converted = true;
            }
            else
            {
                return Fail("Could not recognize cookies. Export Netscape cookies.txt from an extension, or paste the Cookie request header (name=value; name2=value2).");
            }

            return WriteCookies(netscape, converted);
        }

        public YouTubeCookiesSaveResult SaveUploadedFile(string sourcePath)
        {
            if (sourcePath.IsNullOrWhiteSpace() || !_diskProvider.FileExists(sourcePath))
            {
                return Fail("Uploaded cookies file was not found.");
            }

            string content;
            try
            {
                content = File.ReadAllText(sourcePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed reading uploaded cookies");
                return Fail("Failed to read uploaded cookies: " + ex.Message);
            }

            return SaveRaw(content);
        }

        public YouTubeCookiesSaveResult ImportFromKnownLocations()
        {
            if (_configService.YoutubeCookiesPath.IsNotNullOrWhiteSpace() &&
                _diskProvider.FileExists(_configService.YoutubeCookiesPath))
            {
                return new YouTubeCookiesSaveResult
                {
                    Success = true,
                    Path = _configService.YoutubeCookiesPath,
                    Message = "Cookies already configured"
                };
            }

            foreach (var candidate in GetImportCandidates())
            {
                if (!_diskProvider.FileExists(candidate))
                {
                    continue;
                }

                try
                {
                    var content = File.ReadAllText(candidate);
                    if (!LooksLikeCookies(content))
                    {
                        continue;
                    }

                    // Point config at the mounted/shared file when it already lives under /cookies.
                    if (candidate.StartsWith("/cookies/", StringComparison.OrdinalIgnoreCase))
                    {
                        _configService.YoutubeCookiesPath = candidate;
                        _logger.Info("Using YouTube cookies from {0}", candidate);
                        return new YouTubeCookiesSaveResult
                        {
                            Success = true,
                            Path = candidate,
                            Message = $"Using cookies from {candidate}"
                        };
                    }

                    return SaveRaw(content);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to import cookies from {0}", candidate);
                }
            }

            return Fail("No cookies.txt found in /cookies or AppData.");
        }

        public bool Clear()
        {
            var path = _configService.YoutubeCookiesPath;
            _configService.YoutubeCookiesPath = string.Empty;

            if (path.IsNullOrWhiteSpace())
            {
                path = DefaultCookiesPath;
            }

            try
            {
                // Only delete MusicTubearr-managed copies; leave Docker bind mounts alone.
                if (path.IsNotNullOrWhiteSpace() &&
                    path.StartsWith(_appFolderInfo.AppDataFolder, StringComparison.OrdinalIgnoreCase) &&
                    _diskProvider.FileExists(path))
                {
                    _diskProvider.DeleteFile(path);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to delete cookies file {0}", path);
            }

            return true;
        }

        private IEnumerable<string> GetImportCandidates()
        {
            yield return Path.Combine(_appFolderInfo.AppDataFolder, "youtube", "cookies.txt");

            foreach (var path in CandidatePaths)
            {
                yield return path;
            }
        }

        private YouTubeCookiesSaveResult WriteCookies(string netscape, bool convertedFromHeader)
        {
            var target = DefaultCookiesPath;
            var dir = Path.GetDirectoryName(target);
            if (dir.IsNotNullOrWhiteSpace())
            {
                _diskProvider.CreateFolder(dir);
            }

            try
            {
                File.WriteAllText(target, netscape.EndsWith("\n") ? netscape : netscape + "\n", new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed writing cookies to {0}", target);
                return Fail("Failed to save cookies: " + ex.Message);
            }

            _configService.YoutubeCookiesPath = target;
            _logger.Info("Saved YouTube cookies to {0} (convertedFromHeader={1})", target, convertedFromHeader);

            var hasLogin = HasLoginCookies(netscape);
            var message = convertedFromHeader
                ? "Converted Cookie header to Netscape cookies.txt"
                : "Saved Netscape cookies.txt";

            if (!hasLogin)
            {
                message += ". Warning: no YouTube login cookies detected — sign in before exporting.";
            }

            return new YouTubeCookiesSaveResult
            {
                Success = true,
                Path = target,
                Message = message,
                ConvertedFromHeader = convertedFromHeader
            };
        }

        private static YouTubeCookiesSaveResult Fail(string message)
        {
            return new YouTubeCookiesSaveResult
            {
                Success = false,
                Message = message
            };
        }

        internal static bool LooksLikeCookies(string content)
        {
            return LooksLikeNetscape(content) || LooksLikeCookieHeader(content);
        }

        internal static bool LooksLikeNetscape(string content)
        {
            if (content.IsNullOrWhiteSpace())
            {
                return false;
            }

            if (content.Contains("Netscape HTTP Cookie File", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("# HttpOnly_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Tab-separated Netscape rows: domain \t flag \t path \t secure \t expiry \t name \t value
            foreach (var line in content.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.IsNullOrWhiteSpace() || trimmed.StartsWith('#'))
                {
                    continue;
                }

                var parts = trimmed.Split('\t');
                if (parts.Length >= 7 && parts[0].Contains("youtube", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (parts.Length >= 7 && (parts[0].StartsWith('.') || parts[0].Contains('.')))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool LooksLikeCookieHeader(string content)
        {
            if (content.IsNullOrWhiteSpace())
            {
                return false;
            }

            var text = content.Trim();
            if (text.StartsWith("Cookie:", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring("Cookie:".Length).Trim();
            }

            if (text.Contains('\t') && LooksLikeNetscape(content))
            {
                return false;
            }

            // name=value pairs separated by ; or newlines
            var pairs = Regex.Matches(text, @"([^=;\s]+)\s*=\s*([^;]+)");
            return pairs.Count >= 1 && text.Contains('=');
        }

        internal static bool HasLoginCookies(string content)
        {
            if (content.IsNullOrWhiteSpace())
            {
                return false;
            }

            foreach (var name in LoginCookieNames)
            {
                if (Regex.IsMatch(content, $@"(?:^|[\t; ]|/ ){Regex.Escape(name)}(?:[\t=])", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    return true;
                }
            }

            return false;
        }

        internal static string EnsureNetscapeHeader(string content)
        {
            var trimmed = content.Trim().TrimStart('\uFEFF');
            if (trimmed.Contains("Netscape HTTP Cookie File", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            return "# Netscape HTTP Cookie File\n# This file was generated by MusicTubearr\n" + trimmed;
        }

        internal static string CookieHeaderToNetscape(string header)
        {
            var text = header.Trim().TrimStart('\uFEFF');
            if (text.StartsWith("Cookie:", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring("Cookie:".Length).Trim();
            }

            // Allow one cookie per line as well as "; " separated
            text = text.Replace('\n', ';').Replace('\r', ';');

            var sb = new StringBuilder();
            sb.AppendLine("# Netscape HTTP Cookie File");
            sb.AppendLine("# This file was generated by MusicTubearr from a Cookie header");
            sb.AppendLine();

            var expiry = DateTimeOffset.UtcNow.AddYears(1).ToUnixTimeSeconds();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var part in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var eq = part.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                var name = part.Substring(0, eq).Trim();
                var value = part.Substring(eq + 1).Trim();
                if (name.IsNullOrWhiteSpace() || !seen.Add(name))
                {
                    continue;
                }

                // domain, includeSubdomains, path, secure, expiry, name, value
                sb.Append(".youtube.com");
                sb.Append('\t');
                sb.Append("TRUE");
                sb.Append('\t');
                sb.Append('/');
                sb.Append('\t');
                sb.Append("TRUE");
                sb.Append('\t');
                sb.Append(expiry);
                sb.Append('\t');
                sb.Append(name);
                sb.Append('\t');
                sb.Append(value);
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
