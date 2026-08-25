using Elsa.Persistence.VNext.Sqlite;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.VNext;
using Elsa.Secrets.Persistence.VNext.Repositories;
using Microsoft.Data.Sqlite;

namespace Elsa.Persistence.VNext.UnitTests;

public class VNextSecretRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly SqliteDocumentStore _store;
    private readonly VNextSecretRepository _repository;

    public VNextSecretRepositoryTests()
    {
        _store = new SqliteDocumentStore(_connection, new SecretPersistenceSchemaProvider().DescribeSchema());
        _repository = new VNextSecretRepository(_store, new StubTenantAccessor(string.Empty));
    }

    [Fact]
    public async Task Repository_PersistsAndListsSecretsThroughDocumentStore()
    {
        await ActivateAsync();
        await _repository.AddAsync(CreateSecret("smtp:password", "SMTP password"));

        var reloaded = await _repository.GetAsync("SMTP:PASSWORD");
        Assert.NotNull(reloaded);
        Assert.Equal("SMTP password", reloaded.DisplayName);
        Assert.Contains("api-key", reloaded.Tags);

        reloaded.DisplayName = "Updated password";
        await _repository.SaveAsync(reloaded);

        var listed = await _repository.ListAsync();
        Assert.Single(listed);
        Assert.Equal("Updated password", listed.Single().DisplayName);
    }

    [Fact]
    public async Task TryAddOrReplaceDeletedAsync_ReplacesOnlyDeletedSecret()
    {
        await ActivateAsync();
        await _repository.AddAsync(CreateSecret("smtp:password", "SMTP password"));

        var activeReplacementResult = await _repository.TryAddOrReplaceDeletedAsync(CreateSecret("SMTP:PASSWORD", "Active replacement"));
        var deleted = CreateSecret("smtp:password", "Deleted password");
        deleted.Status = SecretStatus.Deleted;
        await _repository.SaveAsync(deleted);

        var deletedReplacementResult = await _repository.TryAddOrReplaceDeletedAsync(CreateSecret("SMTP:PASSWORD", "Replacement password"));
        var reloaded = await _repository.GetAsync("smtp:password");

        Assert.False(activeReplacementResult);
        Assert.True(deletedReplacementResult);
        Assert.NotNull(reloaded);
        Assert.Equal("Replacement password", reloaded.DisplayName);
        Assert.Equal(SecretStatus.Active, reloaded.Status);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private async Task ActivateAsync()
    {
        await _connection.OpenAsync();
        await _store.MaterializeAsync();
    }

    private static Secret CreateSecret(string name, string displayName)
    {
        return new Secret
        {
            Name = name.Trim().ToLowerInvariant(),
            DisplayName = displayName,
            TypeName = SecretTypeNames.Text,
            StoreName = SecretStoreNames.Encrypted,
            Tags = ["API-Key"],
            Versions =
            {
                new SecretVersion
                {
                    Version = 1,
                    Payload = new SecretPayload { Value = "stored", Metadata = { ["protectedValue"] = "ciphertext" } }
                }
            }
        };
    }

    [Theory]
    // The provider keys documents by name alone, so serving a tenant would hand back another tenant's secret.
    // The default-tenant path needs no case of its own: every other test in this class runs through the same
    // guard with an empty tenant id, which is what a single-tenant deployment does.
    [InlineData("tenant-a")]
    [InlineData("*")]
    public async Task RefusesToServeANonDefaultTenant(string tenantId)
    {
        var repository = new VNextSecretRepository(_store, new StubTenantAccessor(tenantId));

        await Assert.ThrowsAsync<NotSupportedException>(() => repository.ListAsync());
        await Assert.ThrowsAsync<NotSupportedException>(() => repository.GetAsync("anything"));
        await Assert.ThrowsAsync<NotSupportedException>(() => repository.AddAsync(new Secret { Name = "a", DisplayName = "a" }));
    }


    private sealed class StubTenantAccessor(string tenantId) : Elsa.Common.Multitenancy.ITenantAccessor
    {
        public string TenantId { get; } = tenantId;
        public Elsa.Common.Multitenancy.Tenant? Tenant => null;
        public IDisposable PushContext(Elsa.Common.Multitenancy.Tenant? tenant) => throw new NotSupportedException();
    }
}
