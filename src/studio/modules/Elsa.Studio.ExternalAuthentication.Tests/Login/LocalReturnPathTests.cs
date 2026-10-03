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
    public void Normalize_RejectsNineTimesEncodedProtocolRelativeUrls()
    {
        Assert.Equal("/", LocalReturnPath.Normalize(LocalReturnPathCorpus.EncodeTimes("//evil.com", 9)));
        Assert.Equal("/", LocalReturnPath.Normalize("/" + LocalReturnPathCorpus.EncodeTimes("//evil.com", 9)));
    }

    [Fact]
    public void ElsaLogin_RootsBaseRelativePathsAgainstPathBase()
    {
        const string studioBase = "https://studio.example/studio/";

        Assert.Equal("/studio/workflows/x", Elsa.Studio.Login.Pages.Login.Login.ResolveReturnUrl("workflows/x", studioBase));
        Assert.Equal("/studio/workflows/definitions?tab=active", Elsa.Studio.Login.Pages.Login.Login.ResolveReturnUrl("workflows/definitions?tab=active", studioBase));
        Assert.Equal("/studio/c|/windows", Elsa.Studio.Login.Pages.Login.Login.ResolveReturnUrl("c|/windows", studioBase));
        Assert.Equal("/studio/http:evil.com", Elsa.Studio.Login.Pages.Login.Login.ResolveReturnUrl("http:evil.com", studioBase));
        Assert.Equal("/", Elsa.Studio.Login.Pages.Login.Login.ResolveReturnUrl("//evil.com", studioBase));
        Assert.Equal("/workflows/definitions?x=1", Elsa.Studio.Login.Pages.Login.Login.ResolveReturnUrl("/workflows/definitions?x=1", studioBase));
    }
}
