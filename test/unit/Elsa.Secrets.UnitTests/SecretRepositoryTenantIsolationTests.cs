using Elsa.Common.Multitenancy;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Extensions;
using Elsa.Secrets.Models;
using Elsa.Secrets.Options;
using Elsa.Secrets.Repositories;
using Elsa.Tenants.Options;
using Microsoft.Extensions.DependencyInjection;
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
        var options = Microsoft.Extensions.Options.Options.Create(new SecretsOptions());
        _ = new FileSecretRepository(options, null);
        _ = new FileSecretRepository(options, tenantAccessor: new DefaultTenantAccessor());

        var binaryConstructor = typeof(FileSecretRepository).GetConstructor([
            typeof(IOptions<SecretsOptions>),
            typeof(ILogger<FileSecretRepository>)])!;
        Assert.False(binaryConstructor.GetParameters()[1].IsOptional);

        var compatibilityConstructor = typeof(FileSecretRepository).GetConstructor([
            typeof(IOptions<SecretsOptions>),
            typeof(ILogger<FileSecretRepository>),
            typeof(ITenantAccessor)])!;
        Assert.True(compatibilityConstructor.GetParameters()[1].IsOptional);
        Assert.True(compatibilityConstructor.GetParameters()[2].IsOptional);
        Assert.NotNull(typeof(FileSecretRepository).GetConstructor([
            typeof(IOptions<SecretsOptions>),
            typeof(IOptions<TenantsOptions>),
            typeof(ISecretNameValidator),
            typeof(ILogger<FileSecretRepository>),
            typeof(ITenantAccessor)]));
        Assert.NotNull(typeof(InMemorySecretRepository).GetConstructor(Type.EmptyTypes));
        Assert.NotNull(typeof(InMemorySecretRepository).GetConstructor([typeof(ITenantAccessor)]));
        Assert.NotNull(typeof(InMemorySecretRepository).GetConstructor([
            typeof(IOptions<TenantsOptions>),
            typeof(ISecretNameValidator),
            typeof(ITenantAccessor)]));
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
            await AssertNullAndEmptyAreDuplicatesAsync(accessor, new FileSecretRepository(options, null, accessor));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task Repositories_NormalizeNamesForLookupAndUniqueness()
    {
        await ForEachRepositoryAsync(async (tenantAccessor, repository) =>
        {
            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                await repository.AddAsync(new Secret { Name = " smtp:password ", DisplayName = "Original" });
                Assert.Equal("Original", (await repository.GetAsync(" SMTP:PASSWORD "))!.DisplayName);

                await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(new Secret
                {
                    Name = "SMTP:PASSWORD",
                    DisplayName = "Duplicate"
                }));

                await repository.SaveAsync(new Secret { Name = " SMTP:PASSWORD ", DisplayName = "Updated" });
                Assert.Equal("Updated", (await repository.GetAsync("smtp:password"))!.DisplayName);
                Assert.False(await repository.TryAddOrReplaceDeletedAsync(new Secret
                {
                    Name = " smtp:password ",
                    DisplayName = "Active duplicate"
                }));
            }

            using (UseTenant(tenantAccessor, "tenant-b"))
            {
                await repository.AddAsync(new Secret { Name = "SMTP:PASSWORD", DisplayName = "Tenant B" });
                Assert.Equal("Tenant B", (await repository.GetAsync(" smtp:password "))!.DisplayName);
            }
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Repositories_CreatedThroughDiFollowTenancyOption(bool tenancyEnabled)
    {
        await ForEachDiRepositoryAsync(tenancyEnabled, async (tenantAccessor, repository) =>
        {
            using (UseTenant(tenantAccessor, "tenant-a"))
                await repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "Tenant A" });

            using (UseTenant(tenantAccessor, "tenant-b"))
            {
                if (tenancyEnabled)
                {
                    Assert.Null(await repository.GetAsync("smtp:password"));
                    await repository.AddAsync(new Secret { Name = "SMTP:PASSWORD", DisplayName = "Tenant B" });
                }
                else
                {
                    Assert.Equal("Tenant A", (await repository.GetAsync("smtp:password"))!.DisplayName);
                    await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(new Secret
                    {
                        Name = "SMTP:PASSWORD",
                        DisplayName = "Duplicate"
                    }));

                    await repository.AddAsync(new Secret
                    {
                        Name = "explicit:secret",
                        DisplayName = "Explicit Tenant A",
                        TenantId = "tenant-a"
                    });

                    Assert.Equal("Explicit Tenant A", (await repository.GetAsync("EXPLICIT:SECRET"))!.DisplayName);
                    Assert.Contains(await repository.ListAsync(), x => x.Name == "explicit:secret");
                    await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(new Secret
                    {
                        Name = " explicit:secret ",
                        DisplayName = "Duplicate Explicit",
                        TenantId = "tenant-b"
                    }));
                }
            }
        });
    }

    [Fact]
    public async Task Repositories_WhenTenancyIsDisabledPreserveIncomingTenantOnReplacement()
    {
        await ForEachDiRepositoryAsync(false, async (_, repository) =>
        {
            await repository.AddAsync(new Secret
            {
                Id = "save-existing",
                Name = "save:secret",
                DisplayName = "Original",
                TenantId = "tenant-a"
            });

            await repository.SaveAsync(new Secret
            {
                Id = "save-incoming",
                Name = "SAVE:SECRET",
                DisplayName = "Updated",
                TenantId = "tenant-b"
            });

            var saved = await repository.GetAsync("save:secret");
            Assert.NotNull(saved);
            Assert.Equal("save-existing", saved!.Id);
            Assert.Equal("tenant-b", saved.TenantId);

            await repository.AddAsync(new Secret
            {
                Id = "deleted-existing",
                Name = "replace:secret",
                DisplayName = "Deleted",
                Status = SecretStatus.Deleted,
                TenantId = "tenant-a"
            });

            var replaced = await repository.TryAddOrReplaceDeletedAsync(new Secret
            {
                Id = "replace-incoming",
                Name = "REPLACE:SECRET",
                DisplayName = "Replacement",
                TenantId = "tenant-b"
            });

            Assert.True(replaced);
            var replacement = await repository.GetAsync("replace:secret");
            Assert.NotNull(replacement);
            Assert.Equal("replace-incoming", replacement!.Id);
            Assert.Equal("tenant-b", replacement.TenantId);
        });
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
            var namedRepository = new FileSecretRepository(options, null, namedAccessor);
            using (UseTenant(namedAccessor, "tenant-a"))
                Assert.Empty(await namedRepository.ListAsync());

            var defaultAccessor = new DefaultTenantAccessor();
            var defaultRepository = new FileSecretRepository(options, null, defaultAccessor);
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
            await test(fileAccessor, new FileSecretRepository(options, null, fileAccessor));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static async Task ForEachDiRepositoryAsync(bool tenancyEnabled, Func<ITenantAccessor, ISecretRepository, Task> test)
    {
        await using (var inMemoryProvider = CreateDiRepository<InMemorySecretRepository>(tenancyEnabled))
            await test(inMemoryProvider.GetRequiredService<ITenantAccessor>(), inMemoryProvider.GetRequiredService<ISecretRepository>());

        var path = Path.Join(Path.GetTempPath(), $"elsa-secrets-{Guid.NewGuid():N}.json");
        try
        {
            await using var fileProvider = CreateDiRepository<FileSecretRepository>(tenancyEnabled, path);
            await test(fileProvider.GetRequiredService<ITenantAccessor>(), fileProvider.GetRequiredService<ISecretRepository>());
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static ServiceProvider CreateDiRepository<TRepository>(bool tenancyEnabled, string? repositoryFilePath = null)
        where TRepository : class, ISecretRepository
    {
        return new ServiceCollection()
            .AddSingleton<DefaultTenantAccessor>()
            .AddSingleton<ITenantAccessor>(services => services.GetRequiredService<DefaultTenantAccessor>())
            .AddSecretsServices(options => options.RepositoryFilePath = repositoryFilePath)
            .Configure<TenantsOptions>(options => options.IsEnabled = tenancyEnabled)
            .AddSingleton<ISecretRepository, TRepository>()
            .BuildServiceProvider();
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
