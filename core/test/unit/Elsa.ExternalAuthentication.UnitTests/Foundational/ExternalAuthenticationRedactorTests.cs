using Elsa.ExternalAuthentication.Services;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ExternalAuthenticationRedactorTests
{
    [Fact]
    public void RedactsSecretsTokensAndProviderResponseBodies()
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

        Assert.All(redactedValues, value => Assert.Equal(ExternalAuthenticationRedactor.RedactedValue, value));
        Assert.DoesNotContain(secret, string.Concat(redactedValues));
        Assert.DoesNotContain(token, string.Concat(redactedValues));
        Assert.DoesNotContain(providerResponse, string.Concat(redactedValues));
    }

    [Fact]
    public void RemovesAllRawClaims()
    {
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> claims = new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["email"] = ["operator@example.test"],
            ["sub"] = ["upstream-subject"]
        };

        var result = ExternalAuthenticationRedactor.RedactRawClaims(claims);

        Assert.Empty(result);
    }

    [Fact]
    public void RedactsConfiguredProjectedClaimsWithoutMutatingTheInput()
    {
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> claims = new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["email"] = ["operator@example.test"],
            ["department"] = ["operations"]
        };

        var result = ExternalAuthenticationRedactor.RedactProjectedClaims(claims, new HashSet<string>(StringComparer.Ordinal) { "email" });

        Assert.Equal([ExternalAuthenticationRedactor.RedactedValue], result["email"]);
        Assert.Equal(["operations"], result["department"]);
        Assert.Equal(["operator@example.test"], claims["email"]);
    }
}
