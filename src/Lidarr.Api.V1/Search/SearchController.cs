using System;
using System.Collections.Generic;
using System.Linq;
using Lidarr.Api.V1.Albums;
using Lidarr.Api.V1.Artist;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser;

namespace Lidarr.Api.V1.Search
{
    [V1ApiController]
    public class SearchController : Controller
    {
        private readonly ISearchForNewEntity _searchProxy;
        private readonly IBuildFileNames _fileNameBuilder;

        public SearchController(ISearchForNewEntity searchProxy, IBuildFileNames fileNameBuilder)
        {
            _searchProxy = searchProxy;
            _fileNameBuilder = fileNameBuilder;
        }

        [HttpGet]
        [Produces("application/json")]
        public IEnumerable<SearchResource> Search([FromQuery] string term)
        {
            var searchResults = _searchProxy.SearchForNewEntity(term);
            return MapToResource(searchResults).ToList();
        }

        private IEnumerable<SearchResource> MapToResource(IEnumerable<object> results)
        {
            var id = 1;
            foreach (var result in results)
            {
                var resource = new SearchResource();
                resource.Id = id++;

                if (result is NzbDrone.Core.Music.Artist artist)
                {
                    resource.Artist = artist.ToResource();
                    resource.ForeignId = artist.ForeignArtistId;

                    var poster = artist.Metadata.Value.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);

                    if (poster != null)
                    {
                        resource.Artist.RemotePoster = poster.Url;
                    }

                    resource.Artist.Folder = _fileNameBuilder.GetArtistFolder(artist);
                }
                else if (result is NzbDrone.Core.Music.Album album)
                {
                    // Ensure artist metadata is attached for search cards / add-album modal.
                    if ((album.Artist?.Value == null || album.Artist.Value.Metadata?.Value == null) &&
                        album.ArtistMetadata?.Value != null)
                    {
                        album.Artist = new NzbDrone.Core.Music.Artist
                        {
                            Metadata = album.ArtistMetadata.Value,
                            CleanName = Parser.CleanArtistName(album.ArtistMetadata.Value.Name),
                            SortName = Parser.NormalizeTitle(album.ArtistMetadata.Value.Name)
                        };
                    }

                    resource.Album = album.ToResource();
                    resource.ForeignId = album.ForeignAlbumId;

                    var cover = album.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Cover);

                    if (cover != null)
                    {
                        resource.Album.RemoteCover = cover.Url;
                    }

                    if (resource.Album.Artist != null && album.Artist?.Value != null)
                    {
                        resource.Album.Artist.Folder = _fileNameBuilder.GetArtistFolder(album.Artist.Value);
                    }
                }
                else
                {
                    throw new NotImplementedException("Bad response from search all proxy");
                }

                yield return resource;
            }
        }
    }
}
