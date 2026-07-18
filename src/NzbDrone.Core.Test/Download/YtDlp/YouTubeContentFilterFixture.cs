using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.YtDlp;

namespace NzbDrone.Core.Test.Download.YtDlp
{
    [TestFixture]
    public class YouTubeContentFilterFixture
    {
        [Test]
        public void should_detect_shorts_by_duration()
        {
            var entry = new YtDlpEntry { Id = "abc", Title = "Clip", Duration = 45 };
            YouTubeContentFilter.IsShort(entry).Should().BeTrue();
        }

        [Test]
        public void should_detect_shorts_by_url()
        {
            var entry = new YtDlpEntry
            {
                Id = "abc",
                Title = "Song",
                Duration = 200,
                WebpageUrl = "https://www.youtube.com/shorts/abc"
            };
            YouTubeContentFilter.IsShort(entry).Should().BeTrue();
        }

        [Test]
        public void should_keep_normal_music_video()
        {
            var entry = new YtDlpEntry
            {
                Id = "abc",
                Title = "Artist - Song (Official Video)",
                Duration = 210,
                WebpageUrl = "https://www.youtube.com/watch?v=abc"
            };

            YouTubeContentFilter.IsShort(entry).Should().BeFalse();
            YouTubeContentFilter.ShouldInclude(entry, excludeShorts: true, musicOnly: true).Should().BeTrue();
            YouTubeContentFilter.MusicScore(entry).Should().BeGreaterThan(0);
        }

        [Test]
        public void should_exclude_reaction_when_music_only()
        {
            var entry = new YtDlpEntry
            {
                Id = "abc",
                Title = "REACTING to new album",
                Duration = 600
            };

            YouTubeContentFilter.ShouldInclude(entry, excludeShorts: true, musicOnly: true).Should().BeFalse();
        }

        [Test]
        public void should_boost_music_search_query()
        {
            YouTubeContentFilter.BoostMusicSearchQuery("Daft Punk", true)
                .Should().Contain("official audio");
        }

        [Test]
        public void filter_videos_removes_shorts()
        {
            var config = new Mock<IConfigService>();
            config.SetupGet(c => c.YoutubeExcludeShorts).Returns(true);
            config.SetupGet(c => c.YoutubeMusicOnly).Returns(false);

            var entries = new List<YtDlpEntry>
            {
                new YtDlpEntry { Id = "short", Title = "Quick", Duration = 30 },
                new YtDlpEntry { Id = "song", Title = "Full Song", Duration = 200 }
            };

            var filtered = YouTubeContentFilter.FilterVideos(entries, config.Object);
            filtered.Should().HaveCount(1);
            filtered[0].Id.Should().Be("song");
        }
    }
}
