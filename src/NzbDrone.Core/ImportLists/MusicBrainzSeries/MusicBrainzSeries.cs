using System;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.ImportLists.MusicBrainzSeries
{
    public class MusicBrainzSeries : HttpImportListBase<MusicBrainzSeriesSettings>
    {
        public override string Name => "MusicBrainz Series (unsupported)";

        public override ProviderMessage Message => new ProviderMessage("This legacy MusicBrainz import list is unsupported in MusicTubearr. Use YouTube-based import lists instead.", ProviderMessageType.Warning);

        public override ImportListType ListType => ImportListType.Other;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);

        private readonly IMetadataRequestBuilder _requestBuilder;

        public MusicBrainzSeries(IHttpClient httpClient, IImportListStatusService importListStatusService, IConfigService configService, IParsingService parsingService, IMetadataRequestBuilder requestBuilder, Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
            _requestBuilder = requestBuilder;
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new MusicBrainzSeriesRequestGenerator(_requestBuilder) { Settings = Settings };
        }

        public override IParseImportListResponse GetParser()
        {
            return new MusicBrainzSeriesParser(Settings);
        }
    }
}
