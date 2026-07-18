using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download.YtDlp;

namespace NzbDrone.Core.Test.Download.YtDlp
{
    [TestFixture]
    public class SponsorBlockCategoriesFixture
    {
        [Test]
        public void music_mode_includes_intro_outro_and_music_offtopic()
        {
            var cats = SponsorBlockCategories.Resolve("music");
            cats.Should().Contain("intro");
            cats.Should().Contain("outro");
            cats.Should().Contain("music_offtopic");
            cats.Should().Contain("sponsor");
        }

        [Test]
        public void off_mode_returns_null()
        {
            SponsorBlockCategories.Resolve("off").Should().BeNull();
        }

        [Test]
        public void custom_mode_uses_provided_categories()
        {
            SponsorBlockCategories.Resolve("custom", "intro, outro")
                .Should().Be("intro,outro");
        }
    }
}
