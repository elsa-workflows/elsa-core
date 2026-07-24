using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Validation;

namespace Elsa.ExternalAuthentication.IntegrationTests.Security;

/// <summary>HTTP broker security contract invariants shared by every browser entry point.</summary>
public class BrokerSecurityTests
{
    [Theory]
    [InlineData("//attacker.example")]
    [InlineData("https://attacker.example")]
    [InlineData("/admin")]
    public void ReturnPathMustBeLocalAndClientAllowlisted(string value)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal) { "/workflows" };

        Assert.False(ClientReturnPathValidator.TryValidateForClient(value, allowed, out _));
    }

    [Fact]
    public void PublicErrorsContainNoProviderOrSecretDetails()
    {
        var error = BrokerErrorFactory.Create(BrokerErrorCategory.AuthenticationFailed);

        Assert.Equal("authentication_failed", error.Error);
        Assert.DoesNotContain("provider", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
