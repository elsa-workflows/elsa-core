using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Notifications;

namespace Elsa.ExternalAuthentication.UnitTests.Notifications;

public class SecurityNotificationTests
{
    [Fact]
    public void EventFamiliesAreImmutableAndCarryOnlyTheSharedSafeContext()
    {
        var notificationTypes = new[]
        {
            typeof(IdentityProviderConnectionChanged), typeof(IdentityProviderConnectionLifecycleChanged),
            typeof(IdentityProviderConnectionSecretBindingChanged), typeof(IdentityProviderConnectionTested),
            typeof(IdentityProviderConnectionPreviewed), typeof(ExternalIdentityLinkChanged),
            typeof(ExternalAuthenticationSessionRevoked), typeof(ExternalSignInCompleted), typeof(ExternalAuthenticationOutcomeRecorded)
        };

        Assert.All(notificationTypes, type =>
        {
            Assert.Contains(type.GetProperties(), property => property.Name == "Context" && property.PropertyType == typeof(SecurityEventContext));
            Assert.DoesNotContain(type.GetProperties(), property => property.Name.Contains("Token", StringComparison.OrdinalIgnoreCase) || property.Name.Contains("Secret", StringComparison.OrdinalIgnoreCase) || property.Name.Contains("Subject", StringComparison.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public void SensitiveStringNeverFormatsItsValueForNotificationsOrLogs()
    {
        using var value = new SensitiveString("a-secret-value");
        Assert.Equal("[REDACTED]", value.ToString());
    }
}
