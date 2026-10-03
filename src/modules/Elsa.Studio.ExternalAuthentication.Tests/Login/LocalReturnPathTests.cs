using Elsa.Studio.Authentication.Abstractions;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Login;

public class LocalReturnPathTests
{
    [Theory]
    [MemberData(nameof(LocalReturnPathCorpus.Cases), MemberType = typeof(LocalReturnPathCorpus))]
    public void Normalize_RejectsOpenRedirectsAndPreservesLocalPaths(string? candidate, string expected)
    {
        Assert.Equal(expected, LocalReturnPath.Normalize(candidate));
        Assert.Equal(expected, Elsa.Studio.ExternalAuthentication.Services.LocalReturnPath.Normalize(candidate));
    }

    [Fact]
    public void Normalize_RejectsDecodedControlCharactersThatCreateProtocolRelativeUrls()
    {
        Assert.Equal("/", LocalReturnPath.Normalize("/\t/evil.com"));
        Assert.Equal("/", LocalReturnPath.Normalize("/\r/evil.com"));
        Assert.Equal("/", LocalReturnPath.Normalize("/\n/evil.com"));
    }

    [Fact]
    public void Normalize_PreservesEncodedLinksByteForByte()
    {
        Assert.Equal("/caf%C3%A9", LocalReturnPath.Normalize("/caf%C3%A9"));
        Assert.Equal("/a?x=%26y", LocalReturnPath.Normalize("/a?x=%26y"));
        Assert.Equal("/a?ref=https%3A%2F%2Fexample.com", LocalReturnPath.Normalize("/a?ref=https%3A%2F%2Fexample.com"));
        Assert.Equal("/a%2Fb", LocalReturnPath.Normalize("/a%2Fb"));
        Assert.Equal("/a?ref=https://example.com", LocalReturnPath.Normalize("/a?ref=https://example.com"));
    }

    [Fact]
    public void Normalize_RejectsNineTimesEncodedProtocolRelativeUrls()
    {
        Assert.Equal("/", LocalReturnPath.Normalize(LocalReturnPathCorpus.EncodeTimes("//evil.com", 9)));
        Assert.Equal("/", LocalReturnPath.Normalize("/" + LocalReturnPathCorpus.EncodeTimes("//evil.com", 9)));
    }
}
