using System;
using System.Linq;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Download.YtDlp
{
    /// <summary>
    /// Maps MusicTubearr SponsorBlock presets to yt-dlp --sponsorblock-remove categories.
    /// See https://github.com/yt-dlp/yt-dlp#sponsorblock-options
    /// </summary>
    public static class SponsorBlockCategories
    {
        public const string MusicDefault = "intro,outro,sponsor,selfpromo,music_offtopic";
        public const string AggressiveDefault = "intro,outro,sponsor,selfpromo,preview,filler,interaction,music_offtopic";

        public static string Resolve(string mode, string customCategories = null)
        {
            if (mode.IsNullOrWhiteSpace())
            {
                mode = "music";
            }

            switch (mode.Trim().ToLowerInvariant())
            {
                case "off":
                case "disabled":
                case "none":
                case "false":
                    return null;

                case "music":
                case "musicvideo":
                case "music_video":
                    return MusicDefault;

                case "aggressive":
                case "all":
                    return AggressiveDefault;

                case "custom":
                    return Normalize(customCategories) ?? MusicDefault;

                default:
                    // Treat unknown mode values as a raw category list for flexibility.
                    return Normalize(mode) ?? MusicDefault;
            }
        }

        public static string Normalize(string categories)
        {
            if (categories.IsNullOrWhiteSpace())
            {
                return null;
            }

            var parts = categories
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.ToLowerInvariant())
                .Where(p => p.Length > 0)
                .Distinct()
                .ToArray();

            return parts.Length == 0 ? null : string.Join(",", parts);
        }
    }
}
