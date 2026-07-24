using System.Text.Json;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ConnectionRevisionCalculatorTests
{
    private readonly ConnectionRevisionCalculator _calculator = new();

    [Fact]
    public void MaterialRevisionIsCanonicalAndIgnoresPresentationOnlyChanges()
    {
        var first = ExternalAuthenticationTestData.CreateConnection();
        var second = ExternalAuthenticationTestData.CreateConnection();
        second.Key = " OIDC ";
        second.AdapterSettings = JsonSerializer.SerializeToElement(new { authority = "https://issuer.example", client = new { id = "studio" } });
        first.AdapterSettings = JsonSerializer.SerializeToElement(new { client = new { id = "studio" }, authority = "https://issuer.example" });
        second.DisplayName = "Corporate login";
        second.DisplayOrder = 99;
        second.IsDefault = true;

        Assert.Equal(_calculator.CalculateMaterialRevision(first), _calculator.CalculateMaterialRevision(second));
    }

    [Fact]
    public void MaterialRevisionChangesWhenAuthenticationMaterialChanges()
    {
        var connection = ExternalAuthenticationTestData.CreateConnection();
        var revision = _calculator.CalculateMaterialRevision(connection);

        connection.SecretBindings["clientSecret"] = new SecretBinding("test", "secret-b");

        Assert.NotEqual(revision, _calculator.CalculateMaterialRevision(connection));
    }

    [Fact]
    public void ConfigurationIdsAreStableForNormalizedKeysAndDistinctPerScope()
    {
        var fromNormalizedKey = ConnectionRevisionCalculator.CalculateConfigurationConnectionId(ConnectionScope.Host, "oidc");
        var fromPaddedKey = ConnectionRevisionCalculator.CalculateConfigurationConnectionId(ConnectionScope.Host, " OIDC ");
        var fromTenantScope = ConnectionRevisionCalculator.CalculateConfigurationConnectionId(new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"), "oidc");

        Assert.Equal(fromNormalizedKey, fromPaddedKey);
        Assert.NotEqual(fromNormalizedKey, fromTenantScope);
        Assert.StartsWith("configuration-", fromNormalizedKey, StringComparison.Ordinal);
    }
}
