using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download.YtDlp;

namespace NzbDrone.Core.Test.Download.YtDlp
{
    [TestFixture]
    public class YouTubeIdsFixture
    {
        [Test]
        public void should_build_and_extract_channel_id()
        {
            var foreign = YouTubeIds.Channel("UCabcdef");
            foreign.Should().Be("yt:channel:UCabcdef");
            YouTubeIds.ExtractChannelId(foreign).Should().Be("UCabcdef");
            YouTubeIds.IsYouTubeId(foreign).Should().BeTrue();
        }

        [Test]
        public void should_build_uploads_and_playlist_ids()
        {
            YouTubeIds.Uploads("UCabcdef").Should().Be("yt:uploads:UCabcdef");
            YouTubeIds.IsUploadsAlbum("yt:uploads:UCabcdef").Should().BeTrue();
            YouTubeIds.ExtractPlaylistId("yt:playlist:PLxyz").Should().Be("PLxyz");
        }

        [Test]
        public void should_extract_video_id_from_urls()
        {
            YouTubeIds.ExtractVideoId("yt:video:dQw4w9WgXcQ").Should().Be("dQw4w9WgXcQ");
            YouTubeIds.ExtractVideoId("https://www.youtube.com/watch?v=dQw4w9WgXcQ").Should().Be("dQw4w9WgXcQ");
            YouTubeIds.ExtractVideoId("https://youtu.be/dQw4w9WgXcQ").Should().Be("dQw4w9WgXcQ");
            YouTubeIds.ToVideoUrl("dQw4w9WgXcQ").Should().Contain("dQw4w9WgXcQ");
        }

        [Test]
        public void should_build_channel_and_playlist_urls()
        {
            YouTubeIds.ToChannelUrl("UCabcdef").Should().Be("https://www.youtube.com/channel/UCabcdef");
            YouTubeIds.ToUploadsUrl("yt:channel:UCabcdef").Should().Contain("/videos");
            YouTubeIds.ToPlaylistUrl("PLxyz").Should().Contain("list=PLxyz");
        }
    }
}
