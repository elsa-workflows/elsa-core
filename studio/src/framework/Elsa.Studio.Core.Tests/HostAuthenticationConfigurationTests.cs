using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Elsa.Studio.Core.Tests;

public class HostAuthenticationConfigurationTests
{
    [Fact]
    public void ServerHostShipsWithRunnableAuthenticationDefaults()
    {
        var appSettings = CssContractTestContext.ReadRepositoryFile(
            "src", "hosts", "Elsa.Studio.Host.Server", "appsettings.json");
        using var document = JsonDocument.Parse(appSettings);
        var authentication = document.RootElement.GetProperty("Authentication");

        Assert.Equal("ElsaIdentity", authentication.GetProperty("Provider").GetString());
        Assert.Equal(string.Empty, authentication
            .GetProperty("ExternalAuthentication")
            .GetProperty("ClientSecret")
            .GetString());
    }

    [Fact]
    public void ServerHostPersistsAntiforgeryStateForSignOut()
    {
        var host = CssContractTestContext.ReadRepositoryFile(
            "src", "hosts", "Elsa.Studio.Host.Server", "Pages", "_Host.cshtml");
        var markup = Regex.Replace(host, @"@\*.*?\*@|<!--.*?-->", string.Empty, RegexOptions.Singleline);

        Assert.Matches(@"<persist-component-state\s*/>", markup);
    }
}
