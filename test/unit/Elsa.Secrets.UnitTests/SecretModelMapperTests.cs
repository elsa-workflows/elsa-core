using Elsa.Secrets.Models;
using Elsa.Secrets.Services;

namespace Elsa.Secrets.UnitTests;

public class SecretModelMapperTests
{
    [Test]
    public async Task ToModel_ReportsExpired_WhenActiveSecretHasOnlyExpiredVersions()
    {
        var secret = new Secret
        {
            Name = "smtp:password",
            DisplayName = "SMTP password",
            Versions =
            {
                new SecretVersion
                {
                    Version = 1,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
                }
            }
        };

        var model = secret.ToModel();

        await Assert.That(model.Status).IsEqualTo(SecretStatus.Expired);
        await Assert.That(model.CurrentVersion).IsNull();
        await Assert.That(model.ExpiresAt).IsNull();
    }
}
