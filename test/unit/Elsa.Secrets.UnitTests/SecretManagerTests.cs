using Elsa.Secrets.Models;
using Xunit;

namespace Elsa.Secrets.UnitTests;

public class SecretManagerTests
{
    private readonly SecretTestFixture _fixture = new();

    [Fact]
    public async Task CreateAsync_NormalizesTechnicalName_AndDoesNotExposeValueInModel()
    {
        var secret = await _fixture.Manager.CreateAsync(new CreateSecretRequest
        {
            Name = "Smtp:Password",
            DisplayName = "SMTP password",
            Value = "p@ssword"
        });

        var model = Elsa.Secrets.Services.SecretModelMapper.ToModel(secret);

        Assert.Equal("smtp:password", secret.Name);
        Assert.Equal("SMTP password", model.DisplayName);
        Assert.Equal(1, model.CurrentVersion);
        Assert.DoesNotContain(model.GetType().GetProperties(), x => x.Name.Contains("Value", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RotateAsync_RetiresPreviousVersion_AndKeepsOneActiveVersion()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });
        var rotated = await _fixture.Manager.RotateAsync("smtp:password", new RotateSecretRequest { Value = "two" });

        Assert.Equal(2, rotated.Versions.Count);
        Assert.Single(rotated.Versions, x => x.Status == SecretStatus.Active);
        Assert.Single(rotated.Versions, x => x.Status == SecretStatus.Retired);
    }

    [Fact]
    public async Task RevokeAsync_PreventsResolution()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });
        await _fixture.Manager.RevokeAsync("smtp:password");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Resolver.ResolveAsync("smtp:password"));
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateTechnicalName()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = " SMTP:PASSWORD ", Value = "two" }));
    }

    [Fact]
    public async Task CountAsync_ReturnsTotalMatchingItems_NotPageSize()
    {
        for (var i = 0; i < 3; i++)
            await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = $"smtp:password:{i}", Value = "one" });

        var items = await _fixture.Manager.ListAsync(new ListSecretsRequest { PageSize = 1 });
        var count = await _fixture.Manager.CountAsync(new ListSecretsRequest { PageSize = 1 });

        Assert.Single(items);
        Assert.Equal(3, count);
    }
}
