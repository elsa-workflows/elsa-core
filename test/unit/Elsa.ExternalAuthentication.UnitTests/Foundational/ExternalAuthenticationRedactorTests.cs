using Elsa.ExternalAuthentication.Services;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ExternalAuthenticationRedactorTests
{
    [Test]
    public async Task RedactsSecretsTokensAndProviderResponseBodies()
    {
        const string secret = "client-secret-value";
        const string token = "access-token-value";
        const string providerResponse = "{\"id_token\":\"raw-identity-token\"}";

        var redactedValues = new[]
        {
            ExternalAuthenticationRedactor.RedactSecret(secret),
            ExternalAuthenticationRedactor.RedactToken(token),
            ExternalAuthenticationRedactor.RedactProviderResponseBody(providerResponse)
        };

        foreach (var value in redactedValues)
            await Assert.That(value).IsEqualTo(ExternalAuthenticationRedactor.RedactedValue);
        await Assert.That(string.Concat(redactedValues)).DoesNotContain(secret);
        await Assert.That(string.Concat(redactedValues)).DoesNotContain(token);
        await Assert.That(string.Concat(redactedValues)).DoesNotContain(providerResponse);
    }

    [Test]
    public async Task RemovesAllRawClaims()
    {
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> claims = new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["email"] = ["operator@example.test"],
            ["sub"] = ["upstream-subject"]
        };

        var result = ExternalAuthenticationRedactor.RedactRawClaims(claims);

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task RedactsConfiguredProjectedClaimsWithoutMutatingTheInput()
    {
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> claims = new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["email"] = ["operator@example.test"],
            ["department"] = ["operations"]
        };

        var result = ExternalAuthenticationRedactor.RedactProjectedClaims(claims, new HashSet<string>(StringComparer.Ordinal) { "email" });

        await Assert.That(result["email"]).IsEquivalentTo(
            [ExternalAuthenticationRedactor.RedactedValue],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(result["department"]).IsEquivalentTo(
            ["operations"],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(claims["email"]).IsEquivalentTo(
            ["operator@example.test"],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }
}
