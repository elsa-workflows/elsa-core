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
    public async Task ManagedGeneration_CannotBeChangedOrResolvedThroughGenericApis()
    {
        var managed = await _fixture.ManagedManager.CreateGenerationAsync("connection-1", "generation-1", "access=synthetic;refresh=synthetic");

        Assert.Equal("connection-1", managed.ManagedOwnerId);
        Assert.Equal("generation-1", managed.ManagedGenerationId);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Resolver.ResolveAsync(managed.Name));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Manager.UpdateAsync(managed.Name, new UpdateSecretRequest { Description = "changed" }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Manager.RotateAsync(managed.Name, new RotateSecretRequest { Value = "replacement" }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Manager.RevokeAsync(managed.Name));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Manager.DeleteAsync(managed.Name));

        var testResult = await _fixture.Manager.TestAsync(managed.Name);
        Assert.False(testResult.Succeeded);
        Assert.Equal("Lifecycle-managed secret generations can only be accessed through their owner.", testResult.Error);
    }

    [Fact]
    public async Task ManagedGeneration_RequiresExactOwnerAndGenerationForResolutionAndCleanup()
    {
        var managed = await _fixture.ManagedManager.CreateGenerationAsync("connection-1", "generation-1", "access=synthetic;refresh=synthetic");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.ManagedManager.ResolveGenerationAsync(managed.Name, "connection-2", "generation-1"));
        Assert.False(await _fixture.ManagedManager.DeleteGenerationAsync(managed.Name, "connection-1", "generation-2"));
        Assert.Equal("access=synthetic;refresh=synthetic", (await _fixture.ManagedManager.ResolveGenerationAsync(managed.Name, "connection-1", "generation-1")).Value);

        Assert.True(await _fixture.ManagedManager.DeleteGenerationAsync(managed.Name, "connection-1", "generation-1"));
        var deleted = await _fixture.Repository.GetAsync(managed.Name);
        Assert.Equal(SecretStatus.Deleted, deleted!.Status);
        Assert.Equal("connection-1", deleted.ManagedOwnerId);
        Assert.Equal("generation-1", deleted.ManagedGenerationId);
    }

    [Fact]
    public async Task GenericCreate_CannotReuseDeletedManagedGenerationName()
    {
        var managed = await _fixture.ManagedManager.CreateGenerationAsync("connection-1", "generation-1", "access=synthetic;refresh=synthetic");
        Assert.True(await _fixture.ManagedManager.DeleteGenerationAsync(managed.Name, "connection-1", "generation-1"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = managed.Name, Value = "generic" }));

        var stored = await _fixture.Repository.GetAsync(managed.Name);
        Assert.Equal("connection-1", stored!.ManagedOwnerId);
        Assert.Equal("generation-1", stored.ManagedGenerationId);
    }

    [Fact]
    public async Task ResolvePayloadAsync_UsesPersistedOwnershipMarkerInsteadOfCallerObject()
    {
        var managed = await _fixture.ManagedManager.CreateGenerationAsync("connection-1", "generation-1", "synthetic-token");
        var forged = new Secret { Id = managed.Id, Name = managed.Name };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Manager.ResolvePayloadAsync(forged));
    }

    [Fact]
    public async Task ManagedGenerations_AreHiddenFromGenericListsAndApiModels()
    {
        var managed = await _fixture.ManagedManager.CreateGenerationAsync("connection-1", "generation-1", "synthetic-token");
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "ordinary:secret", Value = "ordinary-value" });

        var page = await _fixture.Manager.ListPageAsync(new ListSecretsRequest());
        var count = await _fixture.Manager.CountAsync(new ListSecretsRequest());
        var model = Elsa.Secrets.Services.SecretModelMapper.ToModel(managed);

        Assert.Equal(1, count);
        Assert.Equal("ordinary:secret", Assert.Single(page.Items).Name);
        Assert.DoesNotContain(model.GetType().GetProperties(), x => x.Name.Contains("Managed", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(model.GetType().GetProperties(), x => x.Name.Contains("Value", StringComparison.OrdinalIgnoreCase));

        var stored = await _fixture.Repository.GetAsync(managed.Name);
        Assert.DoesNotContain("synthetic-token", System.Text.Json.JsonSerializer.Serialize(stored!.Versions.Single().Payload.Metadata));
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
    public async Task UpdateAsync_UpdatesMetadataWithoutChangingIdentityOrVersion()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest
        {
            Name = "smtp:password",
            DisplayName = "SMTP password",
            Description = "Old description",
            Scope = "production",
            Value = "one"
        });

        var updated = await _fixture.Manager.UpdateAsync("smtp:password", new UpdateSecretRequest
        {
            DisplayName = "  SMTP credential  ",
            Description = "  Rotated manually  "
        });

        Assert.Equal("smtp:password", updated.Name);
        Assert.Equal("SMTP credential", updated.DisplayName);
        Assert.Equal("Rotated manually", updated.Description);
        Assert.Equal("production", updated.Scope);
        Assert.Equal(SecretTypeNames.Text, updated.TypeName);
        Assert.Equal(SecretStoreNames.Encrypted, updated.StoreName);
        Assert.Single(updated.Versions);
        Assert.NotNull(updated.UpdatedAt);
        Assert.Equal("one", await _fixture.Resolver.ResolveAsync("smtp:password"));
    }

    [Fact]
    public async Task UpdateAsync_UsesTechnicalNameAsFallbackDisplayName_AndClearsBlankDescription()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest
        {
            Name = "smtp:password",
            DisplayName = "SMTP password",
            Description = "Old description",
            Value = "one"
        });

        var updated = await _fixture.Manager.UpdateAsync("smtp:password", new UpdateSecretRequest
        {
            DisplayName = " ",
            Description = " "
        });

        Assert.Equal("smtp:password", updated.DisplayName);
        Assert.Null(updated.Description);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenSecretDoesNotExist()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _fixture.Manager.UpdateAsync("missing:secret", new UpdateSecretRequest
        {
            DisplayName = "Missing",
            Description = "Missing"
        }));
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
