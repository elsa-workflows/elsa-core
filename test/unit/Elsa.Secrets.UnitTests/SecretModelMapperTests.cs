using Elsa.Secrets.Models;
using Elsa.Secrets.Services;
using Xunit;

namespace Elsa.Secrets.UnitTests;

public class SecretModelMapperTests
{
    [Fact]
    public void ToModel_ReportsExpired_WhenActiveSecretHasOnlyExpiredVersions()
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

        Assert.Equal(SecretStatus.Expired, model.Status);
        Assert.Null(model.CurrentVersion);
        Assert.Null(model.ExpiresAt);
    }
}
