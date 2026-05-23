using Elsa.Secrets.Models;
using Xunit;

namespace Elsa.Secrets.UnitTests;

public class SecretManagerTests
{
    private readonly SecretTestFixture _fixture = new();

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("1secret")]
    [InlineData("secret name")]
    public async Task CreateAsync_RejectsInvalidTechnicalNames(string name)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = name, Value = "one" }));
    }

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
    public async Task RotateAsync_RejectsRevokedSecret()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });
        await _fixture.Manager.RevokeAsync("smtp:password");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Manager.RotateAsync("smtp:password", new RotateSecretRequest { Value = "two" }));
        var secret = await _fixture.Manager.GetAsync("smtp:password");
        Assert.Equal(SecretStatus.Revoked, secret!.Status);
        Assert.All(secret.Versions, x => Assert.Equal(SecretStatus.Revoked, x.Status));
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateTechnicalName()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = " SMTP:PASSWORD ", Value = "two" }));
    }

    [Fact]
    public async Task CreateAsync_AllowsReusingDeletedSecretName()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });
        await _fixture.Manager.DeleteAsync("smtp:password");

        var secret = await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = " SMTP:PASSWORD ", Value = "two" });

        Assert.Equal("smtp:password", secret.Name);
        Assert.Equal(SecretStatus.Active, secret.Status);
        Assert.Single(secret.Versions);
        Assert.Equal("two", await _fixture.Resolver.ResolveAsync("smtp:password"));
    }

    [Fact]
    public async Task CreateAsync_AllowsEncryptedCertificateWithThumbprintMetadata()
    {
        var secret = await _fixture.Manager.CreateAsync(new CreateSecretRequest
        {
            Name = "tls:certificate",
            TypeName = SecretTypeNames.X509Certificate,
            Value = " ",
            Metadata = new Dictionary<string, string> { ["thumbprint"] = "ABC123" }
        });

        Assert.Equal(SecretTypeNames.X509Certificate, secret.TypeName);
        Assert.Equal("ABC123", secret.Versions.Single().Payload.Metadata["thumbprint"]);
    }

    [Theory]
    [InlineData(SecretTypeNames.Text, SecretStoreNames.Encrypted, null, null)]
    [InlineData(SecretTypeNames.RsaKey, SecretStoreNames.Encrypted, " ", null)]
    [InlineData(SecretTypeNames.RsaKey, SecretStoreNames.Configuration, null, " ")]
    [InlineData(SecretTypeNames.X509Certificate, SecretStoreNames.Encrypted, " ", null)]
    [InlineData(SecretTypeNames.X509Certificate, SecretStoreNames.Configuration, null, " ")]
    public async Task CreateAsync_RejectsInvalidPayloadForTypeAndStore(string typeName, string storeName, string? value, string? configurationKey)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Manager.CreateAsync(new CreateSecretRequest
        {
            Name = $"secret:{Guid.NewGuid():N}",
            TypeName = typeName,
            StoreName = storeName,
            Value = value,
            ConfigurationKey = configurationKey
        }));
    }

    [Fact]
    public async Task RotateAsync_RejectsInvalidReplacementPayload()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Manager.RotateAsync("smtp:password", new RotateSecretRequest()));
    }

    [Fact]
    public async Task CreateAsync_AllowsOnlyOneConcurrentReuseOfDeletedSecretName()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });
        await _fixture.Manager.DeleteAsync("smtp:password");
        var createTasks = Enumerable.Range(0, 2)
            .Select(x => TryCreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = x.ToString() }))
            .ToArray();

        var results = await Task.WhenAll(createTasks);
        var stored = await _fixture.Manager.GetAsync("smtp:password");

        Assert.Single(results, true);
        Assert.NotNull(stored);
        Assert.Equal(SecretStatus.Active, stored.Status);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEncryptedPayloadMaterial()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });

        await _fixture.Manager.DeleteAsync("smtp:password");
        var stored = await _fixture.Repository.GetAsync("smtp:password");

        Assert.NotNull(stored);
        Assert.Equal(SecretStatus.Deleted, stored.Status);
        Assert.All(stored.Versions, x => Assert.False(x.Payload.Metadata.ContainsKey("protectedValue")));
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

    [Fact]
    public async Task ListPageAsync_AppliesFiltersBeforePaging()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest
        {
            Name = "smtp:password",
            DisplayName = "SMTP Password",
            Description = "Production credential",
            Scope = "Production",
            Value = "one"
        });
        await _fixture.Manager.CreateAsync(new CreateSecretRequest
        {
            Name = "api:key",
            DisplayName = "API Key",
            Description = "Production credential",
            Scope = "production",
            Value = "two"
        });
        await _fixture.Manager.CreateAsync(new CreateSecretRequest
        {
            Name = "dev:token",
            Description = "Development credential",
            Scope = "development",
            Value = "three"
        });
        await _fixture.Manager.CreateAsync(new CreateSecretRequest
        {
            Name = "old:credential",
            Description = "Production credential",
            Scope = "production",
            Value = "four"
        });
        await _fixture.Manager.RevokeAsync("old:credential");

        var page = await _fixture.Manager.ListPageAsync(new ListSecretsRequest
        {
            Search = "credential",
            TypeNames = [SecretTypeNames.Text],
            StoreNames = [SecretStoreNames.Encrypted],
            Scope = "PRODUCTION",
            Status = SecretStatus.Active,
            Page = 1,
            PageSize = 1
        });

        Assert.Equal(2, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("smtp:password", page.Items.Single().Name);
    }

    [Fact]
    public async Task TestAsync_ReturnsFailedResult_WhenSecretDoesNotExist()
    {
        var result = await _fixture.Manager.TestAsync("missing:secret");

        Assert.False(result.Succeeded);
        Assert.Contains("missing:secret", result.Error);
    }

    private async Task<bool> TryCreateAsync(CreateSecretRequest request)
    {
        try
        {
            await _fixture.Manager.CreateAsync(request);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
