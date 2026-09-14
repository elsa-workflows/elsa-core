using Elsa.Common.Multitenancy;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Elsa.Secrets.Options;
using Elsa.Secrets.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Secrets.UnitTests;

/// <summary>
/// The default repositories share the EF Secrets tenancy contract: ambient tenant stamping, visibility,
/// and case-insensitive name uniqueness are the same for File and InMemory storage.
/// </summary>
public class SecretRepositoryTenantIsolationTests
{
    [Fact]
    public void Repositories_RetainPreTenancyConstructorShapes()
    {
        Assert.NotNull(typeof(FileSecretRepository).GetConstructor([
            typeof(IOptions<SecretsOptions>),
            typeof(ILogger<FileSecretRepository>)]));
        Assert.NotNull(typeof(FileSecretRepository).GetConstructor([
            typeof(IOptions<SecretsOptions>),
            typeof(ILogger<FileSecretRepository>),
            typeof(ITenantAccessor)]));
        Assert.NotNull(typeof(InMemorySecretRepository).GetConstructor(Type.EmptyTypes));
        Assert.NotNull(typeof(InMemorySecretRepository).GetConstructor([typeof(ITenantAccessor)]));
    }

    [Fact]
    public async Task Repositories_StampAndIsolateSecretsByTenant()
    {
        await ForEachRepositoryAsync(async (tenantAccessor, repository) =>
        {
            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                var secretA = new Secret { Name = "SMTP:PASSWORD", DisplayName = "Tenant A" };
                await repository.AddAsync(secretA);

                Assert.Equal("tenant-a", secretA.TenantId);
                var loadedA = await repository.GetAsync("smtp:password");
                Assert.Equal("Tenant A", loadedA!.DisplayName);
                Assert.Equal("tenant-a", loadedA.TenantId);
                Assert.Single(await repository.ListAsync());

                await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(new Secret
                {
                    Name = "smtp:password",
                    DisplayName = "Duplicate"
                }));
            }

            using (UseTenant(tenantAccessor, "tenant-b"))
            {
                var secretB = new Secret { Name = "smtp:password", DisplayName = "Tenant B" };
                await repository.AddAsync(secretB);

                Assert.Equal("tenant-b", secretB.TenantId);
                Assert.Equal("Tenant B", (await repository.GetAsync("SMTP:PASSWORD"))!.DisplayName);
                Assert.Single(await repository.ListAsync());
            }

            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                Assert.Equal("Tenant A", (await repository.GetAsync("smtp:password"))!.DisplayName);
                Assert.Single(await repository.ListAsync());
            }
        });
    }

    [Fact]
    public async Task Repositories_SaveDoesNotOverwriteAnotherTenantsSecret()
    {
        await ForEachRepositoryAsync(async (tenantAccessor, repository) =>
        {
            using (UseTenant(tenantAccessor, "tenant-a"))
                await repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "Tenant A" });

            using (UseTenant(tenantAccessor, "tenant-b"))
                await repository.SaveAsync(new Secret { Name = "SMTP:PASSWORD", DisplayName = "Tenant B" });

            using (UseTenant(tenantAccessor, "tenant-a"))
                Assert.Equal("Tenant A", (await repository.GetAsync("smtp:password"))!.DisplayName);

            using (UseTenant(tenantAccessor, "tenant-b"))
                Assert.Equal("Tenant B", (await repository.GetAsync("smtp:password"))!.DisplayName);
        });
    }

    [Fact]
    public async Task Repositories_RejectExplicitHiddenTenantDuplicatesWithoutMutation()
    {
        await ForEachRepositoryAsync(async (tenantAccessor, repository) =>
        {
            using (UseTenant(tenantAccessor, "tenant-b"))
                await repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "Tenant B", TenantId = "tenant-b" });

            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(new Secret
                {
                    Name = "SMTP:PASSWORD",
                    DisplayName = "Duplicate",
                    TenantId = "tenant-b"
                }));
                await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SaveAsync(new Secret
                {
                    Name = "smtp:password",
                    DisplayName = "Duplicate",
                    TenantId = "tenant-b"
                }));
                Assert.False(await repository.TryAddOrReplaceDeletedAsync(new Secret
                {
                    Name = "smtp:password",
                    DisplayName = "Duplicate",
                    TenantId = "tenant-b"
                }));
            }

            using (UseTenant(tenantAccessor, "tenant-b"))
                Assert.Equal("Tenant B", (await repository.GetAsync("smtp:password"))!.DisplayName);
        });
    }

    [Fact]
    public async Task Repositories_SaveRetainsTheOwnedTenant()
    {
        await ForEachRepositoryAsync(async (tenantAccessor, repository) =>
        {
            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                await repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "Tenant A" });

                // A caller cannot re-home a named row by putting another tenant on the update payload.
                await repository.SaveAsync(new Secret
                {
                    Name = "SMTP:PASSWORD",
                    DisplayName = "Updated A",
                    TenantId = "tenant-b"
                });

                var updated = await repository.GetAsync("smtp:password");
                Assert.Equal("Updated A", updated!.DisplayName);
                Assert.Equal("tenant-a", updated.TenantId);
            }

            using (UseTenant(tenantAccessor, "tenant-b"))
                Assert.Null(await repository.GetAsync("smtp:password"));
        });
    }

    [Fact]
    public async Task Repositories_TryAddOrReplaceDeletedIsTenantScoped()
    {
        await ForEachRepositoryAsync(async (tenantAccessor, repository) =>
        {
            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                await repository.SaveAsync(new Secret
                {
                    Name = "smtp:password",
                    DisplayName = "Deleted A",
                    Status = SecretStatus.Deleted
                });

                Assert.True(await repository.TryAddOrReplaceDeletedAsync(new Secret
                {
                    Name = "SMTP:PASSWORD",
                    DisplayName = "Replacement A"
                }));
                Assert.Equal("Replacement A", (await repository.GetAsync("smtp:password"))!.DisplayName);
            }

            using (UseTenant(tenantAccessor, "tenant-b"))
            {
                // Tenant B has no visibility into A's deleted row and can create its own row instead.
                Assert.True(await repository.TryAddOrReplaceDeletedAsync(new Secret
                {
                    Name = "smtp:password",
                    DisplayName = "Tenant B"
                }));
                Assert.Equal("Tenant B", (await repository.GetAsync("smtp:password"))!.DisplayName);
            }
        });
    }

    [Fact]
    public async Task Repositories_AgnosticSecretsAreVisibleButNotReplaceableByNamedTenants()
    {
        await ForEachRepositoryAsync(async (tenantAccessor, repository) =>
        {
            using (UseTenant(tenantAccessor, Tenant.AgnosticTenantId))
            {
                await repository.AddAsync(new Secret
                {
                    Name = "shared:secret",
                    DisplayName = "Shared",
                    TenantId = Tenant.AgnosticTenantId
                });
            }

            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                Assert.Equal("Shared", (await repository.GetAsync("SHARED:SECRET"))!.DisplayName);
                await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SaveAsync(new Secret
                {
                    Name = "shared:secret",
                    DisplayName = "Rehomed"
                }));
                Assert.False(await repository.TryAddOrReplaceDeletedAsync(new Secret
                {
                    Name = "shared:secret",
                    DisplayName = "Rehomed"
                }));
            }

            using (UseTenant(tenantAccessor, "tenant-b"))
                Assert.Equal("Shared", (await repository.GetAsync("shared:secret"))!.DisplayName);
        });
    }

    [Fact]
    public async Task Repositories_PreserveNullRowsForLegacyNoAccessorUse()
    {
        var inMemory = new InMemorySecretRepository();
        await inMemory.AddAsync(new Secret { Name = "legacy:secret", DisplayName = "Legacy" });
        var loadedInMemory = await inMemory.GetAsync("legacy:secret");
        Assert.Null(loadedInMemory!.TenantId);

        var path = Path.Join(Path.GetTempPath(), $"elsa-secrets-{Guid.NewGuid():N}.json");
        try
        {
            var file = new FileSecretRepository(Microsoft.Extensions.Options.Options.Create(new SecretsOptions { RepositoryFilePath = path }));
            await file.AddAsync(new Secret { Name = "legacy:secret", DisplayName = "Legacy" });
            var reloaded = new FileSecretRepository(Microsoft.Extensions.Options.Options.Create(new SecretsOptions { RepositoryFilePath = path }));

            var loadedFile = await reloaded.GetAsync("legacy:secret");
            Assert.Null(loadedFile!.TenantId);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task Repositories_TreatNullAndEmptyTenantIdsAsTheSameDefaultTenantForUniqueness()
    {
        var inMemoryAccessor = new MutableTenantAccessor(null);
        await AssertNullAndEmptyAreDuplicatesAsync(inMemoryAccessor, new InMemorySecretRepository(inMemoryAccessor));

        var path = Path.Join(Path.GetTempPath(), $"elsa-secrets-{Guid.NewGuid():N}.json");
        try
        {
            var accessor = new MutableTenantAccessor(null);
            var options = Microsoft.Extensions.Options.Options.Create(new SecretsOptions { RepositoryFilePath = path });
            await AssertNullAndEmptyAreDuplicatesAsync(accessor, new FileSecretRepository(options, tenantAccessor: accessor));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task FileRepository_DefaultTenantKeepsReadingLegacyNullTenantRows()
    {
        var path = Path.Join(Path.GetTempPath(), $"elsa-secrets-{Guid.NewGuid():N}.json");
        try
        {
            var options = Microsoft.Extensions.Options.Options.Create(new SecretsOptions { RepositoryFilePath = path });
            var legacyRepository = new FileSecretRepository(options);
            await legacyRepository.AddAsync(new Secret { Name = "legacy:secret", DisplayName = "Legacy" });

            var namedAccessor = new DefaultTenantAccessor();
            var namedRepository = new FileSecretRepository(options, tenantAccessor: namedAccessor);
            using (UseTenant(namedAccessor, "tenant-a"))
                Assert.Empty(await namedRepository.ListAsync());

            var defaultAccessor = new DefaultTenantAccessor();
            var defaultRepository = new FileSecretRepository(options, tenantAccessor: defaultAccessor);
            Assert.Equal("Legacy", (await defaultRepository.GetAsync("legacy:secret"))!.DisplayName);
            Assert.Null((await defaultRepository.GetAsync("legacy:secret"))!.TenantId);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static IDisposable UseTenant(ITenantAccessor tenantAccessor, string tenantId) =>
        tenantAccessor.PushContext(new Tenant { Id = tenantId, Name = tenantId });

    private static async Task AssertNullAndEmptyAreDuplicatesAsync(MutableTenantAccessor tenantAccessor, ISecretRepository repository)
    {
        await repository.AddAsync(new Secret { Name = "legacy:secret", DisplayName = "Legacy" });
        tenantAccessor.TenantId = "tenant-a";

        var explicitDefault = new Secret
        {
            Name = "LEGACY:SECRET",
            DisplayName = "Duplicate",
            TenantId = Tenant.DefaultTenantId
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(explicitDefault));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SaveAsync(explicitDefault));
        Assert.False(await repository.TryAddOrReplaceDeletedAsync(explicitDefault));
    }

    private static async Task ForEachRepositoryAsync(Func<ITenantAccessor, ISecretRepository, Task> test)
    {
        var inMemoryAccessor = new DefaultTenantAccessor();
        await test(inMemoryAccessor, new InMemorySecretRepository(inMemoryAccessor));

        var path = Path.Join(Path.GetTempPath(), $"elsa-secrets-{Guid.NewGuid():N}.json");
        try
        {
            var fileAccessor = new DefaultTenantAccessor();
            var options = Microsoft.Extensions.Options.Options.Create(new SecretsOptions { RepositoryFilePath = path });
            await test(fileAccessor, new FileSecretRepository(options, tenantAccessor: fileAccessor));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private sealed class MutableTenantAccessor(string? tenantId) : ITenantAccessor
    {
        public string TenantId { get; set; } = tenantId!;

        public Tenant? Tenant => TenantId is null ? null : new Tenant { Id = TenantId, Name = TenantId };

        public IDisposable PushContext(Tenant? tenant)
        {
            var previous = TenantId;
            TenantId = tenant?.Id;
            return new Scope(() => TenantId = previous);
        }

        private sealed class Scope(Action dispose) : IDisposable
        {
            public void Dispose() => dispose();
        }
    }
}
