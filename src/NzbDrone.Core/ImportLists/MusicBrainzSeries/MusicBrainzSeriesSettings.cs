using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.MusicBrainzSeries
{
    public class MusicBrainzSeriesSettingsValidator : AbstractValidator<MusicBrainzSeriesSettings>
    {
    }

    public class MusicBrainzSeriesSettings : IImportListSettings
    {
        private static readonly MusicBrainzSeriesSettingsValidator Validator = new MusicBrainzSeriesSettingsValidator();

        public MusicBrainzSeriesSettings()
        {
            BaseUrl = "";
        }

        public string BaseUrl { get; set; }

        [FieldDefinition(0, Label = "Series Id", HelpText = "Unsupported legacy field. MusicTubearr uses YouTube channels/playlists instead of MusicBrainz series.")]
        public string SeriesId { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
