using Elsa.Common.Multitenancy;
using Elsa.Secrets.Models;
using System.Threading.Tasks;

namespace Elsa.Secrets.UnitTests;

/// <summary>
/// Shared File / InMemory / EF Core store-contract assertions for secret repositories.
/// Covers per-tenant name uniqueness and ambient-tenant isolation on the existing
/// <see cref="Elsa.Secrets.Contracts.ISecretRepository"/> surface.
/// </summary>
public abstract class SecretRepositoryConformanceTests
{
    protected abstract Task<SecretRepositoryScenario> CreateScenarioAsync();

    [Test]
    public async Task TwoTenantsCanOwnTheSameSecretName()
    {
        await using var scenario = await CreateScenarioAsync();

        using (scenario.UseTenant("tenant-a"))
        {
            var secretA = CreateSecret("SMTP:PASSWORD", "Tenant A");
            await scenario.Repository.AddAsync(secretA);

            await Assert.That(secretA.TenantId).IsEqualTo("tenant-a");
            await Assert.That(() => scenario.Repository.AddAsync(CreateSecret("smtp:password", "Duplicate A")))
                .ThrowsExactly<InvalidOperationException>();
        }

        using (scenario.UseTenant("tenant-b"))
        {
            var secretB = CreateSecret("smtp:password", "Tenant B");
            await scenario.Repository.AddAsync(secretB);

            await Assert.That(secretB.TenantId).IsEqualTo("tenant-b");
            await Assert.That((await scenario.Repository.GetAsync("SMTP:PASSWORD"))!.DisplayName).IsEqualTo("Tenant B");
        }

        using (scenario.UseTenant("tenant-a"))
            await Assert.That((await scenario.Repository.GetAsync("smtp:password"))!.DisplayName).IsEqualTo("Tenant A");
    }

    [Test]
    public async Task GetDoesNotCrossReadAnotherTenant()
    {
        await using var scenario = await CreateScenarioAsync();

        using (scenario.UseTenant("tenant-a"))
            await scenario.Repository.AddAsync(CreateSecret("smtp:password", "Tenant A"));

        using (scenario.UseTenant("tenant-b"))
        {
            await Assert.That(await scenario.Repository.GetAsync("smtp:password")).IsNull();
            await scenario.Repository.AddAsync(CreateSecret("smtp:password", "Tenant B"));
        }

        using (scenario.UseTenant("tenant-a"))
        {
            var loaded = await scenario.Repository.GetAsync("SMTP:PASSWORD");
            var existing = await Assert.That(loaded).IsNotNull();
            await Assert.That(existing.DisplayName).IsEqualTo("Tenant A");
            await Assert.That(existing.TenantId).IsEqualTo("tenant-a");
        }
    }

    [Test]
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
            var onlySecret = await Assert.That(listedB).HasSingleItem();
            await Assert.That(onlySecret.DisplayName).IsEqualTo("Tenant B");
        }

        using (scenario.UseTenant("tenant-a"))
        {
            var listedA = (await scenario.Repository.ListAsync()).OrderBy(x => x.Name).ToList();
            await Assert.That(listedA.Select(x => x.Name).ToList()).IsEquivalentTo(["api:key", "smtp:password"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
            foreach (var secret in listedA)
                await Assert.That(secret.TenantId).IsEqualTo("tenant-a");
        }
    }

    [Test]
    public async Task DeletedReplaceStaysPerTenant()
    {
        await using var scenario = await CreateScenarioAsync();

        using (scenario.UseTenant("tenant-a"))
        {
            await scenario.Repository.SaveAsync(CreateSecret("smtp:password", "Deleted A", SecretStatus.Deleted));
            await Assert.That(await scenario.Repository.TryAddOrReplaceDeletedAsync(CreateSecret("SMTP:PASSWORD", "Replacement A"))).IsTrue();
            await Assert.That((await scenario.Repository.GetAsync("smtp:password"))!.DisplayName).IsEqualTo("Replacement A");
        }

        using (scenario.UseTenant("tenant-b"))
        {
            await Assert.That(await scenario.Repository.TryAddOrReplaceDeletedAsync(CreateSecret("smtp:password", "Tenant B"))).IsTrue();
            await Assert.That((await scenario.Repository.GetAsync("smtp:password"))!.DisplayName).IsEqualTo("Tenant B");
        }

        using (scenario.UseTenant("tenant-a"))
            await Assert.That((await scenario.Repository.GetAsync("smtp:password"))!.DisplayName).IsEqualTo("Replacement A");
    }

    [Test]
    public async Task DefaultTenantRejectsDuplicateNames()
    {
        await using var scenario = await CreateScenarioAsync();

        using (scenario.UseTenant(Tenant.DefaultTenantId))
        {
            var secret = CreateSecret("smtp:password", "Default");
            await scenario.Repository.AddAsync(secret);
            await Assert.That(secret.TenantId).IsEqualTo(Tenant.DefaultTenantId);

            await Assert.That(() => scenario.Repository.AddAsync(CreateSecret("SMTP:PASSWORD", "Duplicate")))
                .ThrowsExactly<InvalidOperationException>();
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

[InheritsTests]
public sealed class InMemorySecretRepositoryConformanceTests : SecretRepositoryConformanceTests
{
    protected override Task<SecretRepositoryScenario> CreateScenarioAsync() =>
        SecretRepositoryScenario.CreateInMemoryAsync();
}

[InheritsTests]
public sealed class FileSecretRepositoryConformanceTests : SecretRepositoryConformanceTests
{
    protected override Task<SecretRepositoryScenario> CreateScenarioAsync() =>
        SecretRepositoryScenario.CreateFileAsync();
}

[InheritsTests]
public sealed class SqliteSecretRepositoryConformanceTests : SecretRepositoryConformanceTests
{
    protected override Task<SecretRepositoryScenario> CreateScenarioAsync() =>
        SecretRepositoryScenario.CreateSqliteAsync();
}
