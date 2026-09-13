using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Notifications;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Notifications;

public class SecurityNotificationTests
{
    [Test]
    public async Task EventFamiliesAreImmutableAndCarryOnlyTheSharedSafeContext()
    {
        var notificationTypes = new[]
        {
            typeof(IdentityProviderConnectionChanged), typeof(IdentityProviderConnectionLifecycleChanged),
            typeof(IdentityProviderConnectionSecretBindingChanged), typeof(IdentityProviderConnectionTested),
            typeof(IdentityProviderConnectionPreviewed), typeof(ExternalIdentityLinkChanged), typeof(ExternalIdentityLinkReplaced),
            typeof(ExternalAuthenticationSessionRevoked), typeof(ExternalAuthenticationConnectionSessionsRevoked),
            typeof(ExternalSignInCompleted), typeof(ExternalAuthenticationOutcomeRecorded)
        };

        foreach (var type in notificationTypes)
        {
            await Assert.That(type.GetProperties()).Contains(property => property.Name == "Context" && property.PropertyType == typeof(SecurityEventContext));
            await Assert.That(type.GetProperties()).DoesNotContain(property => property.Name.Contains("Token", StringComparison.OrdinalIgnoreCase) || property.Name.Contains("Secret", StringComparison.OrdinalIgnoreCase) || property.Name.Contains("Subject", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Test]
    public async Task SensitiveStringNeverFormatsItsValueForNotificationsOrLogs()
    {
        using var value = new SensitiveString("a-secret-value");
        await Assert.That(value.ToString()).IsEqualTo("[REDACTED]");
    }
}
