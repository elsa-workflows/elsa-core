using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Providers;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ConfigurationIdentityProviderConnectionSourceTests
{
    [Fact]
    public async Task MaterializesOnlyRequestedScopeWithStableGeneratedIdAndSnapshotVersion()
    {
        var options = new ExternalAuthenticationOptions
        {
            ConfigurationConnections =
            [
                RegistryTestData.Connection(string.Empty, "*", " Contoso ", displayName: "Contoso"),
                RegistryTestData.Connection("tenant", "tenant-a", "fabrikam")
            ]
        };
        var monitor = new MutableOptionsMonitor<ExternalAuthenticationOptions>(options);
        var calculator = new ConnectionRevisionCalculator();
        var source = new ConfigurationIdentityProviderConnectionSource(monitor, calculator);

        var first = await source.GetSnapshotAsync(ConnectionScope.Host);
        var second = await source.GetSnapshotAsync(ConnectionScope.Host);

        var connection = Assert.Single(first.Connections);
        Assert.Equal("contoso", connection.Key);
        Assert.Equal(ConnectionRevisionCalculator.CalculateConfigurationConnectionId(ConnectionScope.Host, "contoso"), connection.Id);
        Assert.Equal(first.Version, second.Version);
        Assert.StartsWith("m-", connection.MaterialRevision);
    }

    [Fact]
    public async Task DoesNotReturnMutableConfigurationObjects()
    {
        var configuredConnection = RegistryTestData.Connection("connection");
        var source = new ConfigurationIdentityProviderConnectionSource(
            new MutableOptionsMonitor<ExternalAuthenticationOptions>(new ExternalAuthenticationOptions { ConfigurationConnections = [configuredConnection] }),
            new ConnectionRevisionCalculator());

        var snapshot = await source.GetSnapshotAsync(ConnectionScope.Host);
        var materializedConnection = Assert.Single(snapshot.Connections);
        materializedConnection.DisplayName = "Changed";

        Assert.Equal("Contoso", configuredConnection.DisplayName);
    }
}
