using Elsa.Studio.Authentication.Abstractions;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Login;

public class LocalReturnPathTests
{
    [Theory]
    [InlineData("/%09/evil.com", "/")]
    [InlineData("//evil.com", "/")]
    [InlineData("/\\evil.com", "/")]
    [InlineData("\\\\evil.com", "/")]
    [InlineData("/%2F/evil.com", "/")]
    [InlineData("http://evil.com", "/")]
    [InlineData("https://evil.com/phish", "/")]
    [InlineData("/workflows/definitions?x=1", "/workflows/definitions?x=1")]
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
}
