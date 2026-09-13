using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Validation;

namespace Elsa.ExternalAuthentication.IntegrationTests.Security;

/// <summary>HTTP broker security contract invariants shared by every browser entry point.</summary>
public class BrokerSecurityTests
{
    [Test]
    [Arguments("//attacker.example")]
    [Arguments("https://attacker.example")]
    [Arguments("/admin")]
    public async Task ReturnPathMustBeLocalAndClientAllowlisted(string value)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal) { "/workflows" };

        await Assert.That(ClientReturnPathValidator.TryValidateForClient(value, allowed, out _)).IsFalse();
    }

    [Test]
    public async Task PublicErrorsContainNoProviderOrSecretDetails()
    {
        var error = BrokerErrorFactory.Create(BrokerErrorCategory.AuthenticationFailed);

        await Assert.That(error.Error).IsEqualTo("authentication_failed");
        await Assert.That(error.Message).DoesNotContain("provider").WithComparison(StringComparison.OrdinalIgnoreCase);
        await Assert.That(error.Message).DoesNotContain("secret").WithComparison(StringComparison.OrdinalIgnoreCase);
    }
}
