using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Download.Clients.YtDlp
{
    public class YtDlpSettingsValidator : AbstractValidator<YtDlpSettings>
    {
        public YtDlpSettingsValidator()
        {
            RuleFor(c => c.DownloadFolder).IsValidPath();
        }
    }

    public class YtDlpSettings : IProviderConfig
    {
        private static readonly YtDlpSettingsValidator Validator = new YtDlpSettingsValidator();

        public YtDlpSettings()
        {
            DownloadFolder = "";
            AudioFormat = "";
            AudioQuality = "0";
            OutputTemplate = "%(title)s.%(ext)s";
            ExtraArguments = "--extractor-args youtube:player_client=default,android,web";
        }

        [FieldDefinition(0, Label = "Download Folder", Type = FieldType.Path, HelpText = "Folder where yt-dlp writes completed audio files for import")]
        public string DownloadFolder { get; set; }

        [FieldDefinition(1, Label = "yt-dlp Path", HelpText = "Optional override for yt-dlp executable path")]
        public string YtDlpPath { get; set; }

        [FieldDefinition(2, Label = "FFmpeg Path", HelpText = "Optional override for ffmpeg directory or binary path")]
        public string FfmpegPath { get; set; }

        [FieldDefinition(3, Label = "Cookies File", Type = FieldType.Path, HelpText = "Optional Netscape cookies.txt path (falls back to MusicTubearr YouTube settings)")]
        public string CookiesPath { get; set; }

        [FieldDefinition(4, Label = "Audio Format", HelpText = "Target audio format for ffmpeg (mp3, aac, alac, m4a, flac, opus, wav). Use aac or alac for iTunes. Leave empty to use global YouTube setting.")]
        public string AudioFormat { get; set; }

        [FieldDefinition(5, Label = "Audio Quality", HelpText = "yt-dlp --audio-quality (0 = best)", Advanced = true)]
        public string AudioQuality { get; set; }

        [FieldDefinition(6, Label = "Output Template", HelpText = "yt-dlp output filename template", Advanced = true)]
        public string OutputTemplate { get; set; }

        [FieldDefinition(7, Label = "Extra Arguments", HelpText = "Additional yt-dlp arguments", Advanced = true)]
        public string ExtraArguments { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
