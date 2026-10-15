using Elsa.Common.Multitenancy;
using Elsa.Secrets.Models;

namespace Elsa.Secrets.UnitTests;

/// <summary>
/// Shared File / InMemory / EF Core store-contract assertions for secret repositories.
/// Covers per-tenant name uniqueness and ambient-tenant isolation on the existing
/// <see cref="Elsa.Secrets.Contracts.ISecretRepository"/> surface.
/// </summary>
public abstract class SecretRepositoryConformanceTests
{
    protected abstract Task<SecretRepositoryScenario> CreateScenarioAsync();

    [Fact]
    public async Task TwoTenantsCanOwnTheSameSecretName()
    {
        await using var scenario = await CreateScenarioAsync();

        using (scenario.UseTenant("tenant-a"))
        {
            var secretA = CreateSecret("SMTP:PASSWORD", "Tenant A");
            await scenario.Repository.AddAsync(secretA);

            Assert.Equal("tenant-a", secretA.TenantId);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scenario.Repository.AddAsync(CreateSecret("smtp:password", "Duplicate A")));
        }

        using (scenario.UseTenant("tenant-b"))
        {
            var secretB = CreateSecret("smtp:password", "Tenant B");
            await scenario.Repository.AddAsync(secretB);

            Assert.Equal("tenant-b", secretB.TenantId);
            Assert.Equal("Tenant B", (await scenario.Repository.GetAsync("SMTP:PASSWORD"))!.DisplayName);
        }

        using (scenario.UseTenant("tenant-a"))
            Assert.Equal("Tenant A", (await scenario.Repository.GetAsync("smtp:password"))!.DisplayName);
    }

    [Fact]
    public async Task GetDoesNotCrossReadAnotherTenant()
    {
        await using var scenario = await CreateScenarioAsync();

        using (scenario.UseTenant("tenant-a"))
            await scenario.Repository.AddAsync(CreateSecret("smtp:password", "Tenant A"));

        using (scenario.UseTenant("tenant-b"))
        {
            Assert.Null(await scenario.Repository.GetAsync("smtp:password"));
            await scenario.Repository.AddAsync(CreateSecret("smtp:password", "Tenant B"));
        }

        using (scenario.UseTenant("tenant-a"))
        {
            var loaded = await scenario.Repository.GetAsync("SMTP:PASSWORD");
            Assert.NotNull(loaded);
            Assert.Equal("Tenant A", loaded.DisplayName);
            Assert.Equal("tenant-a", loaded.TenantId);
        }
    }

    [Fact]
    public async Task ListIsTenantScoped()
    {
        await using var scenario = await CreateScenarioAsync();

        using (scenario.UseTenant("tenant-a"))
        {
            await scenario.Repository.AddAsync(CreateSecret("smtp:password", "Tenant A"));
            await scenario.Repository.AddAsync(CreateSecret("api:key", "Key A"));
        }

        using (scenario.UseTenant("tenant-b"))
        {
            await scenario.Repository.AddAsync(CreateSecret("smtp:password", "Tenant B"));
            var listedB = await scenario.Repository.ListAsync();
            Assert.Equal("Tenant B", Assert.Single(listedB).DisplayName);
        }

        using (scenario.UseTenant("tenant-a"))
        {
            var listedA = (await scenario.Repository.ListAsync()).OrderBy(x => x.Name).ToList();
            Assert.Equal(["api:key", "smtp:password"], listedA.Select(x => x.Name).ToList());
            Assert.All(listedA, secret => Assert.Equal("tenant-a", secret.TenantId));
        }
    }

    [Fact]
    public async Task DeletedReplaceStaysPerTenant()
    {
        await using var scenario = await CreateScenarioAsync();

        using (scenario.UseTenant("tenant-a"))
        {
            await scenario.Repository.SaveAsync(CreateSecret("smtp:password", "Deleted A", SecretStatus.Deleted));
            Assert.True(await scenario.Repository.TryAddOrReplaceDeletedAsync(CreateSecret("SMTP:PASSWORD", "Replacement A")));
            Assert.Equal("Replacement A", (await scenario.Repository.GetAsync("smtp:password"))!.DisplayName);
        }

        using (scenario.UseTenant("tenant-b"))
        {
            Assert.True(await scenario.Repository.TryAddOrReplaceDeletedAsync(CreateSecret("smtp:password", "Tenant B")));
            Assert.Equal("Tenant B", (await scenario.Repository.GetAsync("smtp:password"))!.DisplayName);
        }

        using (scenario.UseTenant("tenant-a"))
            Assert.Equal("Replacement A", (await scenario.Repository.GetAsync("smtp:password"))!.DisplayName);
    }

    [Fact]
    public async Task DefaultTenantRejectsDuplicateNames()
    {
        await using var scenario = await CreateScenarioAsync();

        using (scenario.UseTenant(Tenant.DefaultTenantId))
        {
            var secret = CreateSecret("smtp:password", "Default");
            await scenario.Repository.AddAsync(secret);
            Assert.Equal(Tenant.DefaultTenantId, secret.TenantId);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scenario.Repository.AddAsync(CreateSecret("SMTP:PASSWORD", "Duplicate")));
        }
    }

    private static Secret CreateSecret(string name, string displayName, SecretStatus status = SecretStatus.Active) =>
        new()
        {
            Name = name,
            DisplayName = displayName,
            Status = status
        };
}

public sealed class InMemorySecretRepositoryConformanceTests : SecretRepositoryConformanceTests
{
    protected override Task<SecretRepositoryScenario> CreateScenarioAsync() =>
        SecretRepositoryScenario.CreateInMemoryAsync();
}

public sealed class FileSecretRepositoryConformanceTests : SecretRepositoryConformanceTests
{
    protected override Task<SecretRepositoryScenario> CreateScenarioAsync() =>
        SecretRepositoryScenario.CreateFileAsync();
}

public sealed class SqliteSecretRepositoryConformanceTests : SecretRepositoryConformanceTests
{
    protected override Task<SecretRepositoryScenario> CreateScenarioAsync() =>
        SecretRepositoryScenario.CreateSqliteAsync();
}
