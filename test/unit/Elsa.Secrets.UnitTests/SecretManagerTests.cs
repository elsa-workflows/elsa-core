using Elsa.Secrets.Models;

namespace Elsa.Secrets.UnitTests;

public class SecretManagerTests
{
    private readonly SecretTestFixture _fixture = new();

    [Test]
    [Arguments("")]
    [Arguments("a")]
    [Arguments("1secret")]
    [Arguments("secret name")]
    public async Task CreateAsync_RejectsInvalidTechnicalNames(string name)
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = name, Value = "one" }));
    }

    [Test]
    public async Task CreateAsync_NormalizesTechnicalName_AndDoesNotExposeValueInModel()
    {
        var secret = await _fixture.Manager.CreateAsync(new CreateSecretRequest
        {
            Name = "Smtp:Password",
            DisplayName = "SMTP password",
            Value = "p@ssword"
        });

        var model = Elsa.Secrets.Services.SecretModelMapper.ToModel(secret);

        await Assert.That(secret.Name).IsEqualTo("smtp:password");
        await Assert.That(model.DisplayName).IsEqualTo("SMTP password");
        await Assert.That(model.CurrentVersion).IsEqualTo(1);
        await Assert.That(model.GetType().GetProperties()).DoesNotContain(x => x.Name.Contains("Value", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task RotateAsync_RetiresPreviousVersion_AndKeepsOneActiveVersion()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });
        var rotated = await _fixture.Manager.RotateAsync("smtp:password", new RotateSecretRequest { Value = "two" });

        await Assert.That(rotated.Versions.Count).IsEqualTo(2);
        await Assert.That(rotated.Versions).HasSingleItem(x => x.Status == SecretStatus.Active);
        await Assert.That(rotated.Versions).HasSingleItem(x => x.Status == SecretStatus.Retired);
    }

    [Test]
    public async Task RevokeAsync_PreventsResolution()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });
        await _fixture.Manager.RevokeAsync("smtp:password");

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _fixture.Resolver.ResolveAsync("smtp:password"));
    }

    [Test]
    public async Task RotateAsync_RejectsRevokedSecret()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });
        await _fixture.Manager.RevokeAsync("smtp:password");

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _fixture.Manager.RotateAsync("smtp:password", new RotateSecretRequest { Value = "two" }));
        var secret = await Assert.That(await _fixture.Manager.GetAsync("smtp:password")).IsNotNull();
        await Assert.That(secret.Status).IsEqualTo(SecretStatus.Revoked);
        foreach (var version in secret.Versions)
            await Assert.That(version.Status).IsEqualTo(SecretStatus.Revoked);
    }

    [Test]
    public async Task CreateAsync_RejectsDuplicateTechnicalName()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = " SMTP:PASSWORD ", Value = "two" }));
    }

    [Test]
    public async Task CreateAsync_AllowsReusingDeletedSecretName()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });
        await _fixture.Manager.DeleteAsync("smtp:password");

        var secret = await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = " SMTP:PASSWORD ", Value = "two" });

        await Assert.That(secret.Name).IsEqualTo("smtp:password");
        await Assert.That(secret.Status).IsEqualTo(SecretStatus.Active);
        await Assert.That(secret.Versions).HasSingleItem();
        await Assert.That(await _fixture.Resolver.ResolveAsync("smtp:password")).IsEqualTo("two");
    }

    [Test]
    public async Task CreateAsync_AllowsEncryptedCertificateWithThumbprintMetadata()
    {
        var secret = await _fixture.Manager.CreateAsync(new CreateSecretRequest
        {
            Name = "tls:certificate",
            TypeName = SecretTypeNames.X509Certificate,
            Value = " ",
            Metadata = new Dictionary<string, string> { ["thumbprint"] = "ABC123" }
        });

        await Assert.That(secret.TypeName).IsEqualTo(SecretTypeNames.X509Certificate);
        await Assert.That(secret.Versions.Single().Payload.Metadata["thumbprint"]).IsEqualTo("ABC123");
    }

    [Test]
    [Arguments(SecretTypeNames.Text, SecretStoreNames.Encrypted, null, null)]
    [Arguments(SecretTypeNames.RsaKey, SecretStoreNames.Encrypted, " ", null)]
    [Arguments(SecretTypeNames.RsaKey, SecretStoreNames.Configuration, null, " ")]
    [Arguments(SecretTypeNames.X509Certificate, SecretStoreNames.Encrypted, " ", null)]
    [Arguments(SecretTypeNames.X509Certificate, SecretStoreNames.Configuration, null, " ")]
    public async Task CreateAsync_RejectsInvalidPayloadForTypeAndStore(string typeName, string storeName, string? value, string? configurationKey)
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _fixture.Manager.CreateAsync(new CreateSecretRequest
        {
            Name = $"secret:{Guid.NewGuid():N}",
            TypeName = typeName,
            StoreName = storeName,
            Value = value,
            ConfigurationKey = configurationKey
        }));
    }

    [Test]
    public async Task RotateAsync_RejectsInvalidReplacementPayload()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _fixture.Manager.RotateAsync("smtp:password", new RotateSecretRequest()));
    }

    [Test]
    public async Task CreateAsync_AllowsOnlyOneConcurrentReuseOfDeletedSecretName()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });
        await _fixture.Manager.DeleteAsync("smtp:password");
        var createTasks = Enumerable.Range(0, 2)
            .Select(x => TryCreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = x.ToString() }))
            .ToArray();

        var results = await Task.WhenAll(createTasks);
        var stored = await Assert.That(await _fixture.Manager.GetAsync("smtp:password")).IsNotNull();

        await Assert.That(results).HasSingleItem(result => result);
        await Assert.That(stored.Status).IsEqualTo(SecretStatus.Active);
    }

    [Test]
    public async Task DeleteAsync_RemovesEncryptedPayloadMaterial()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "smtp:password", Value = "one" });

        await _fixture.Manager.DeleteAsync("smtp:password");
        var stored = await Assert.That(await _fixture.Repository.GetAsync("smtp:password")).IsNotNull();

        await Assert.That(stored.Status).IsEqualTo(SecretStatus.Deleted);
        foreach (var version in stored.Versions)
            await Assert.That(version.Payload.Metadata.ContainsKey("protectedValue")).IsFalse();
    }

    [Test]
    public async Task CountAsync_ReturnsTotalMatchingItems_NotPageSize()
    {
        for (var i = 0; i < 3; i++)
            await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = $"smtp:password:{i}", Value = "one" });

        var items = await _fixture.Manager.ListAsync(new ListSecretsRequest { PageSize = 1 });
        var count = await _fixture.Manager.CountAsync(new ListSecretsRequest { PageSize = 1 });

        await Assert.That(items).HasSingleItem();
        await Assert.That(count).IsEqualTo(3);
    }

    [Test]
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

        await Assert.That(updated.Name).IsEqualTo("smtp:password");
        await Assert.That(updated.DisplayName).IsEqualTo("SMTP credential");
        await Assert.That(updated.Description).IsEqualTo("Rotated manually");
        await Assert.That(updated.Scope).IsEqualTo("production");
        await Assert.That(updated.TypeName).IsEqualTo(SecretTypeNames.Text);
        await Assert.That(updated.StoreName).IsEqualTo(SecretStoreNames.Encrypted);
        await Assert.That(updated.Versions).HasSingleItem();
        await Assert.That(updated.UpdatedAt).IsNotNull();
        await Assert.That(await _fixture.Resolver.ResolveAsync("smtp:password")).IsEqualTo("one");
    }

    [Test]
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

        await Assert.That(updated.DisplayName).IsEqualTo("smtp:password");
        await Assert.That(updated.Description).IsNull();
    }

    [Test]
    public async Task UpdateAsync_Throws_WhenSecretDoesNotExist()
    {
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _fixture.Manager.UpdateAsync("missing:secret", new UpdateSecretRequest
        {
            DisplayName = "Missing",
            Description = "Missing"
        }));
    }

    [Test]
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

        await Assert.That(page.TotalCount).IsEqualTo(2);
        await Assert.That(page.Items).HasSingleItem();
        await Assert.That(page.Items.Single().Name).IsEqualTo("smtp:password");
    }

    [Test]
    public async Task TestAsync_ReturnsFailedResult_WhenSecretDoesNotExist()
    {
        var result = await _fixture.Manager.TestAsync("missing:secret");

        await Assert.That(result.Succeeded).IsFalse();
        var error = await Assert.That(result.Error).IsNotNull();
        await Assert.That(error).Contains("missing:secret").WithComparison(StringComparison.CurrentCulture);
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
