using System.Text.Json;
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
}
