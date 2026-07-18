using Lidarr.Http.REST;
using NzbDrone.Core.Configuration;

namespace Lidarr.Api.V1.Config
{
    public class MetadataProviderConfigResource : RestResource
    {
        public string MetadataSource { get; set; }
        public WriteAudioTagsType WriteAudioTags { get; set; }
        public bool ScrubAudioTags { get; set; }
        public bool EmbedCoverArt { get; set; }

        // YouTube / yt-dlp (MusicTubearr)
        public string YoutubeCookiesPath { get; set; }
        public string YtDlpPath { get; set; }
        public string FfmpegPath { get; set; }
        public string YoutubeAudioFormat { get; set; }
        public string YoutubeAudioQuality { get; set; }
    }

    public static class MetadataProviderConfigResourceMapper
    {
        public static MetadataProviderConfigResource ToResource(IConfigService model)
        {
            return new MetadataProviderConfigResource
            {
                MetadataSource = model.MetadataSource,
                WriteAudioTags = model.WriteAudioTags,
                ScrubAudioTags = model.ScrubAudioTags,
                EmbedCoverArt = model.EmbedCoverArt,
                YoutubeCookiesPath = model.YoutubeCookiesPath,
                YtDlpPath = model.YtDlpPath,
                FfmpegPath = model.FfmpegPath,
                YoutubeAudioFormat = model.YoutubeAudioFormat,
                YoutubeAudioQuality = model.YoutubeAudioQuality,
            };
        }
    }
}
