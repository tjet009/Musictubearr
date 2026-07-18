using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;

namespace NzbDrone.Core.Download.YtDlp
{
    public class YtDlpService : IYtDlpService
    {
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public YtDlpService(IConfigService configService, Logger logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public string ResolveYtDlpPath(string overridePath = null)
        {
            if (overridePath.IsNotNullOrWhiteSpace())
            {
                return overridePath;
            }

            if (_configService.YtDlpPath.IsNotNullOrWhiteSpace())
            {
                return _configService.YtDlpPath;
            }

            return "yt-dlp";
        }

        public string ResolveFfmpegPath(string overridePath = null)
        {
            if (overridePath.IsNotNullOrWhiteSpace())
            {
                return overridePath;
            }

            if (_configService.FfmpegPath.IsNotNullOrWhiteSpace())
            {
                return _configService.FfmpegPath;
            }

            return "ffmpeg";
        }

        public string ResolveCookiesPath(string overridePath = null)
        {
            if (overridePath.IsNotNullOrWhiteSpace())
            {
                return overridePath;
            }

            return _configService.YoutubeCookiesPath;
        }

        public YtDlpEntry GetJson(string urlOrQuery, int? playlistEnd = null, string cookiesPath = null, string ytDlpPath = null)
        {
            var args = new List<string>
            {
                "--dump-single-json",
                "--no-warnings",
                "--ignore-no-formats-error"
            };

            if (playlistEnd.HasValue)
            {
                args.Add("--playlist-end");
                args.Add(playlistEnd.Value.ToString());
            }

            args.Add("--flat-playlist");
            AddCookies(args, cookiesPath);
            args.Add(urlOrQuery);

            var output = RunYtDlp(args, ytDlpPath);
            if (output.IsNullOrWhiteSpace())
            {
                return null;
            }

            return JsonConvert.DeserializeObject<YtDlpEntry>(output);
        }

        public List<YtDlpEntry> SearchVideos(string query, int maxResults = 20, string cookiesPath = null, string ytDlpPath = null)
        {
            if (query.IsNullOrWhiteSpace())
            {
                return new List<YtDlpEntry>();
            }

            var searchUrl = $"ytsearch{maxResults}:{query}";
            var result = GetJson(searchUrl, maxResults, cookiesPath, ytDlpPath);

            return FlattenEntries(result);
        }

        public List<YtDlpEntry> SearchChannels(string query, int maxResults = 10, string cookiesPath = null, string ytDlpPath = null)
        {
            var artists = new Dictionary<string, YtDlpEntry>(StringComparer.OrdinalIgnoreCase);

            // Prefer channel search filter when possible; fall back to video search grouping.
            try
            {
                var encoded = Uri.EscapeDataString(query);
                var channelSearch = $"https://www.youtube.com/results?search_query={encoded}&sp=EgIQAg%3D%3D";
                var channelResult = GetJson(channelSearch, maxResults, cookiesPath, ytDlpPath);

                foreach (var entry in FlattenEntries(channelResult))
                {
                    var channelId = entry.ResolvedChannelId ?? entry.Id;
                    if (channelId.IsNullOrWhiteSpace())
                    {
                        continue;
                    }

                    if (!channelId.StartsWith("UC", StringComparison.Ordinal) && entry.Type != "channel")
                    {
                        continue;
                    }

                    artists[channelId] = entry;
                    if (artists.Count >= maxResults)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "YouTube channel search failed for '{0}', falling back to video search", query);
            }

            if (artists.Count == 0)
            {
                foreach (var video in SearchVideos(query, Math.Max(maxResults * 3, 15), cookiesPath, ytDlpPath))
                {
                    var channelId = video.ResolvedChannelId;
                    if (channelId.IsNullOrWhiteSpace() || artists.ContainsKey(channelId))
                    {
                        continue;
                    }

                    artists[channelId] = video;
                    if (artists.Count >= maxResults)
                    {
                        break;
                    }
                }
            }

            return artists.Values.ToList();
        }

        public YtDlpEntry GetChannel(string channelIdOrUrl, string cookiesPath = null, string ytDlpPath = null)
        {
            var url = YouTubeIds.ToChannelUrl(channelIdOrUrl);
            return GetJson(url, 1, cookiesPath, ytDlpPath);
        }

        public YtDlpEntry GetChannelUploads(string channelId, int? playlistEnd = 100, string cookiesPath = null, string ytDlpPath = null)
        {
            var url = YouTubeIds.ToUploadsUrl(channelId);
            return GetJson(url, playlistEnd, cookiesPath, ytDlpPath);
        }

        public List<YtDlpEntry> GetChannelPlaylists(string channelId, string cookiesPath = null, string ytDlpPath = null)
        {
            var url = YouTubeIds.ToPlaylistsUrl(channelId);
            var result = GetJson(url, null, cookiesPath, ytDlpPath);
            return FlattenEntries(result);
        }

        public YtDlpEntry GetPlaylist(string playlistIdOrUrl, int? playlistEnd = null, string cookiesPath = null, string ytDlpPath = null)
        {
            var url = YouTubeIds.ToPlaylistUrl(playlistIdOrUrl);
            return GetJson(url, playlistEnd, cookiesPath, ytDlpPath);
        }

        public YtDlpEntry GetVideo(string videoIdOrUrl, string cookiesPath = null, string ytDlpPath = null)
        {
            var url = YouTubeIds.ToVideoUrl(videoIdOrUrl);
            var args = new List<string>
            {
                "--dump-single-json",
                "--no-warnings",
                "--ignore-no-formats-error",
                "--skip-download"
            };

            AddCookies(args, cookiesPath);
            args.Add(url);

            var output = RunYtDlp(args, ytDlpPath);
            return output.IsNullOrWhiteSpace() ? null : JsonConvert.DeserializeObject<YtDlpEntry>(output);
        }

        public async Task DownloadAudioAsync(
            string url,
            string outputDirectory,
            string outputTemplate,
            string audioFormat,
            string audioQuality,
            string cookiesPath = null,
            string ytDlpPath = null,
            string ffmpegPath = null,
            string extraArgs = null,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(outputDirectory);

            var args = new List<string>
            {
                "--no-warnings",
                "-x",
                "--audio-format", audioFormat.IsNullOrWhiteSpace() ? "mp3" : audioFormat,
                "--audio-quality", audioQuality.IsNullOrWhiteSpace() ? "0" : audioQuality,
                "-o", Path.Combine(outputDirectory, outputTemplate),
                "--ffmpeg-location", ResolveFfmpegPath(ffmpegPath),
                "--newline"
            };

            AddCookies(args, cookiesPath);

            if (extraArgs.IsNotNullOrWhiteSpace())
            {
                args.AddRange(SplitArgs(extraArgs));
            }

            args.Add(url);

            await RunYtDlpAsync(args, ytDlpPath, cancellationToken);
        }

        public bool TestExecutable(string ytDlpPath = null, string cookiesPath = null)
        {
            try
            {
                var args = new List<string> { "--version" };
                var version = RunYtDlp(args, ytDlpPath);
                _logger.Info("yt-dlp version: {0}", version?.Trim());
                return version.IsNotNullOrWhiteSpace();
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "yt-dlp test failed");
                return false;
            }
        }

        private void AddCookies(List<string> args, string cookiesPath)
        {
            var resolved = ResolveCookiesPath(cookiesPath);
            if (resolved.IsNotNullOrWhiteSpace() && File.Exists(resolved))
            {
                args.Add("--cookies");
                args.Add(resolved);
            }
        }

        private static List<YtDlpEntry> FlattenEntries(YtDlpEntry root)
        {
            if (root == null)
            {
                return new List<YtDlpEntry>();
            }

            if (root.Entries != null && root.Entries.Count > 0)
            {
                return root.Entries.Where(e => e != null && e.Id.IsNotNullOrWhiteSpace()).ToList();
            }

            if (root.Id.IsNotNullOrWhiteSpace())
            {
                return new List<YtDlpEntry> { root };
            }

            return new List<YtDlpEntry>();
        }

        private string RunYtDlp(List<string> args, string ytDlpPath = null)
        {
            return RunYtDlpAsync(args, ytDlpPath, CancellationToken.None).GetAwaiter().GetResult();
        }

        private async Task<string> RunYtDlpAsync(List<string> args, string ytDlpPath, CancellationToken cancellationToken)
        {
            var executable = ResolveYtDlpPath(ytDlpPath);
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            _logger.Debug("Running yt-dlp: {0} {1}", executable, string.Join(" ", args.Select(QuoteArg)));

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    stdout.AppendLine(e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    stderr.AppendLine(e.Data);
                    _logger.Trace("yt-dlp: {0}", e.Data);
                }
            };

            if (!process.Start())
            {
                throw new DownstreamException(System.Net.HttpStatusCode.BadGateway, "Failed to start yt-dlp");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = stderr.ToString();
                if (error.IsNullOrWhiteSpace())
                {
                    error = stdout.ToString();
                }

                throw new DownstreamException(System.Net.HttpStatusCode.BadGateway,
                    $"yt-dlp failed (exit {process.ExitCode}): {error.Trim()}");
            }

            return stdout.ToString();
        }

        private static IEnumerable<string> SplitArgs(string extraArgs)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            foreach (var ch in extraArgs)
            {
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(ch) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }

                    continue;
                }

                current.Append(ch);
            }

            if (current.Length > 0)
            {
                result.Add(current.ToString());
            }

            return result;
        }

        private static string QuoteArg(string arg)
        {
            return arg.Contains(' ') ? $"\"{arg}\"" : arg;
        }
    }
}
