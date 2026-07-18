using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Download.YtDlp
{
    /// <summary>
    /// Filters YouTube Shorts and non-music clutter from search/indexer/album mapping.
    /// </summary>
    public static class YouTubeContentFilter
    {
        private static readonly Regex ShortsUrlRegex = new Regex(
            @"/(shorts|clip)/",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly string[] MusicBoostTokens =
        {
            "official video",
            "official audio",
            "music video",
            "lyric video",
            "lyrics",
            "visualizer",
            "audio",
            " mv",
            "mv ",
            "(mv)",
            "[mv]",
            "topic",
            "ost",
            "soundtrack"
        };

        private static readonly string[] NonMusicTokens =
        {
            "shorts",
            "#shorts",
            "reaction",
            "reacting",
            "reacts",
            "react to",
            "podcast",
            "interview",
            "trailer",
            "teaser",
            "gameplay",
            "walkthrough",
            "vlog",
            "unboxing",
            "review",
            "tutorial",
            "how to",
            "asmr",
            "comedy skit",
            "podcast episode"
        };

        public const int ShortsMaxSeconds = 60;

        public static List<YtDlpEntry> FilterVideos(IEnumerable<YtDlpEntry> entries, IConfigService config)
        {
            if (entries == null)
            {
                return new List<YtDlpEntry>();
            }

            var excludeShorts = config == null || config.YoutubeExcludeShorts;
            var musicOnly = config != null && config.YoutubeMusicOnly;

            var filtered = entries
                .Where(e => e != null && ShouldInclude(e, excludeShorts, musicOnly))
                .ToList();

            if (musicOnly && filtered.Count > 1)
            {
                filtered = filtered
                    .OrderByDescending(MusicScore)
                    .ThenByDescending(e => e.Duration ?? 0)
                    .ToList();
            }

            return filtered;
        }

        public static bool ShouldInclude(YtDlpEntry entry, bool excludeShorts, bool musicOnly)
        {
            if (entry == null || entry.Id.IsNullOrWhiteSpace())
            {
                return false;
            }

            if (entry.Type == "playlist" || entry.Type == "channel")
            {
                return false;
            }

            if (excludeShorts && IsShort(entry))
            {
                return false;
            }

            if (musicOnly && IsLikelyNonMusic(entry))
            {
                return false;
            }

            if (musicOnly && MusicScore(entry) < 0)
            {
                return false;
            }

            return true;
        }

        public static bool IsShort(YtDlpEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            var url = entry.WebpageUrl ?? entry.Url ?? string.Empty;
            if (ShortsUrlRegex.IsMatch(url))
            {
                return true;
            }

            var title = entry.Title ?? string.Empty;
            if (title.IndexOf("#shorts", StringComparison.OrdinalIgnoreCase) >= 0 ||
                title.IndexOf("[shorts]", StringComparison.OrdinalIgnoreCase) >= 0 ||
                Regex.IsMatch(title, @"\bshorts\b", RegexOptions.IgnoreCase))
            {
                return true;
            }

            // YouTube Shorts are typically <= 60s. Keep unknown-duration items.
            if (entry.Duration.HasValue && entry.Duration.Value > 0 && entry.Duration.Value <= ShortsMaxSeconds)
            {
                return true;
            }

            return false;
        }

        public static bool IsLikelyNonMusic(YtDlpEntry entry)
        {
            var title = (entry?.Title ?? string.Empty).ToLowerInvariant();
            if (title.IsNullOrWhiteSpace())
            {
                return false;
            }

            // Strong music signal wins over weak non-music tokens.
            if (MusicBoostTokens.Any(t => title.Contains(t)))
            {
                return false;
            }

            return NonMusicTokens.Any(t => title.Contains(t));
        }

        public static int MusicScore(YtDlpEntry entry)
        {
            var title = (entry?.Title ?? string.Empty).ToLowerInvariant();
            var score = 0;

            foreach (var token in MusicBoostTokens)
            {
                if (title.Contains(token))
                {
                    score += 3;
                }
            }

            foreach (var token in NonMusicTokens)
            {
                if (title.Contains(token))
                {
                    score -= 4;
                }
            }

            if (entry?.Duration != null)
            {
                var seconds = entry.Duration.Value;
                if (seconds >= 90 && seconds <= 15 * 60)
                {
                    score += 2;
                }
                else if (seconds > 45 * 60)
                {
                    // Long uploads are often podcasts / full lives unless title says so.
                    if (!title.Contains("live") && !title.Contains("concert") && !title.Contains("album"))
                    {
                        score -= 2;
                    }
                }
            }

            if (entry?.Categories != null &&
                entry.Categories.Any(c => c != null && c.Equals("Music", StringComparison.OrdinalIgnoreCase)))
            {
                score += 5;
            }

            return score;
        }

        public static string BoostMusicSearchQuery(string query, bool musicOnly)
        {
            if (!musicOnly || query.IsNullOrWhiteSpace())
            {
                return query;
            }

            var lower = query.ToLowerInvariant();
            if (lower.Contains("official") ||
                lower.Contains("music video") ||
                lower.Contains("audio") ||
                lower.Contains("lyric"))
            {
                return query;
            }

            return query.Trim() + " official audio";
        }
    }
}
