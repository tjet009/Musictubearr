using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download.YtDlp;

namespace NzbDrone.Core.Test.Download.YtDlp
{
    [TestFixture]
    public class YouTubeCookiesServiceFixture
    {
        [Test]
        public void should_detect_netscape_cookies()
        {
            var content = "# Netscape HTTP Cookie File\n.youtube.com\tTRUE\t/\tTRUE\t0\tLOGIN_INFO\tabc\n";
            YouTubeCookiesService.LooksLikeNetscape(content).Should().BeTrue();
            YouTubeCookiesService.HasLoginCookies(content).Should().BeTrue();
        }

        [Test]
        public void should_convert_cookie_header_to_netscape()
        {
            var header = "Cookie: LOGIN_INFO=abc123; SID=xyz; PREF=f1=500";
            YouTubeCookiesService.LooksLikeCookieHeader(header).Should().BeTrue();

            var netscape = YouTubeCookiesService.CookieHeaderToNetscape(header);
            netscape.Should().Contain("Netscape HTTP Cookie File");
            netscape.Should().Contain(".youtube.com");
            netscape.Should().Contain("LOGIN_INFO");
            netscape.Should().Contain("abc123");
            YouTubeCookiesService.HasLoginCookies(netscape).Should().BeTrue();
        }

        [Test]
        public void should_accept_semicolon_pairs_without_cookie_prefix()
        {
            var header = "SAPISID=one; __Secure-1PSID=two";
            YouTubeCookiesService.LooksLikeCookieHeader(header).Should().BeTrue();
            var netscape = YouTubeCookiesService.CookieHeaderToNetscape(header);
            netscape.Should().Contain("SAPISID\tone");
        }
    }
}
