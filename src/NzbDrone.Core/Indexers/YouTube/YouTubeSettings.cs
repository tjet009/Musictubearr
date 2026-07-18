using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.YouTube
{
    public class YouTubeSettingsValidator : AbstractValidator<YouTubeSettings>
    {
        public YouTubeSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).NotEmpty();
        }
    }

    public class YouTubeSettings : IIndexerSettings
    {
        private static readonly YouTubeSettingsValidator Validator = new YouTubeSettingsValidator();

        public YouTubeSettings()
        {
            BaseUrl = "https://www.youtube.com";
            MaxResults = 50;
        }

        [FieldDefinition(0, Label = "Base URL", HelpText = "YouTube base URL")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Type = FieldType.Path, Label = "Cookies File", HelpText = "Path to Netscape cookies.txt for YouTube authentication (optional, falls back to MusicTubearr YouTube settings)")]
        public string CookiesPath { get; set; }

        [FieldDefinition(2, Label = "yt-dlp Path", HelpText = "Path to yt-dlp executable (optional, falls back to global setting)")]
        public string YtDlpPath { get; set; }

        [FieldDefinition(3, Type = FieldType.Number, Label = "Max Results", HelpText = "Maximum videos/releases to return per search")]
        public int MaxResults { get; set; }

        [FieldDefinition(4, Type = FieldType.Number, Label = "Early Download Limit", Unit = "days", HelpText = "Time before release date MusicTubearr will download from this indexer, empty is no limit", Advanced = true)]
        public int? EarlyReleaseLimit { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
