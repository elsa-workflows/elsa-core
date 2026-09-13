using System.Text.Json;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ConnectionRevisionCalculatorTests
{
    private readonly ConnectionRevisionCalculator _calculator = new();

    [Test]
    public async Task MaterialRevisionIsCanonicalAndIgnoresPresentationOnlyChanges()
    {
        var first = ExternalAuthenticationTestData.CreateConnection();
        var second = ExternalAuthenticationTestData.CreateConnection();
        second.Key = " OIDC ";
        second.AdapterSettings = JsonSerializer.SerializeToElement(new { authority = "https://issuer.example", client = new { id = "studio" } });
        first.AdapterSettings = JsonSerializer.SerializeToElement(new { client = new { id = "studio" }, authority = "https://issuer.example" });
        second.DisplayName = "Corporate login";
        second.DisplayOrder = 99;
        second.IsPreferred = true;

        await Assert.That(_calculator.CalculateMaterialRevision(second)).IsEqualTo(_calculator.CalculateMaterialRevision(first));
    }

    [Test]
    public async Task MaterialRevisionChangesWhenAuthenticationMaterialChanges()
    {
        var connection = ExternalAuthenticationTestData.CreateConnection();
        var revision = _calculator.CalculateMaterialRevision(connection);

        connection.SecretBindings["clientSecret"] = new SecretBinding("test", "secret-b");

        await Assert.That(_calculator.CalculateMaterialRevision(connection)).IsNotEqualTo(revision);
    }

    [Test]
    public async Task ConfigurationIdsAreStableForNormalizedKeysAndDistinctPerScope()
    {
        var fromNormalizedKey = ConnectionRevisionCalculator.CalculateConfigurationConnectionId(ConnectionScope.Host, "oidc");
        var fromPaddedKey = ConnectionRevisionCalculator.CalculateConfigurationConnectionId(ConnectionScope.Host, " OIDC ");
        var fromTenantScope = ConnectionRevisionCalculator.CalculateConfigurationConnectionId(new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"), "oidc");

        await Assert.That(fromPaddedKey).IsEqualTo(fromNormalizedKey);
        await Assert.That(fromTenantScope).IsNotEqualTo(fromNormalizedKey);
        await Assert.That(fromNormalizedKey).StartsWith("configuration-").WithComparison(StringComparison.Ordinal);
    }
}
