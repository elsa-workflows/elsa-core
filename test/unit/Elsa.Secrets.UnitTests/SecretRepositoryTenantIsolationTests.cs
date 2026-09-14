using System.IO;
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
using System.Threading.Tasks;

namespace Elsa.Secrets.UnitTests;

/// <summary>
/// The default repositories share the EF Secrets tenancy contract: ambient tenant stamping, visibility,
/// and case-insensitive name uniqueness are the same for File and InMemory storage.
/// </summary>
public class SecretRepositoryTenantIsolationTests
{
    [Test]
    public async Task Repositories_RetainPreTenancyConstructorShapes()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new SecretsOptions());
        _ = new FileSecretRepository(options, null);
        _ = new FileSecretRepository(options, tenantAccessor: new DefaultTenantAccessor());

        var binaryConstructor = await Assert.That(typeof(FileSecretRepository).GetConstructor([
            typeof(IOptions<SecretsOptions>),
            typeof(ILogger<FileSecretRepository>)])).IsNotNull();
        await Assert.That(binaryConstructor.GetParameters()[1].IsOptional).IsFalse();

        var compatibilityConstructor = await Assert.That(typeof(FileSecretRepository).GetConstructor([
            typeof(IOptions<SecretsOptions>),
            typeof(ILogger<FileSecretRepository>),
            typeof(ITenantAccessor)])).IsNotNull();
        await Assert.That(compatibilityConstructor.GetParameters()[1].IsOptional).IsTrue();
        await Assert.That(compatibilityConstructor.GetParameters()[2].IsOptional).IsTrue();
        await Assert.That(typeof(FileSecretRepository).GetConstructor([
            typeof(IOptions<SecretsOptions>),
            typeof(IOptions<TenantsOptions>),
            typeof(ISecretNameValidator),
            typeof(ILogger<FileSecretRepository>),
            typeof(ITenantAccessor)])).IsNotNull();
        await Assert.That(typeof(InMemorySecretRepository).GetConstructor(Type.EmptyTypes)).IsNotNull();
        await Assert.That(typeof(InMemorySecretRepository).GetConstructor([typeof(ITenantAccessor)])).IsNotNull();
        await Assert.That(typeof(InMemorySecretRepository).GetConstructor([
            typeof(IOptions<TenantsOptions>),
            typeof(ISecretNameValidator),
            typeof(ITenantAccessor)])).IsNotNull();
    }

    [Test]
    public async Task Repositories_StampAndIsolateSecretsByTenant()
    {
        await ForEachRepositoryAsync(async (tenantAccessor, repository) =>
        {
            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                var secretA = new Secret { Name = "SMTP:PASSWORD", DisplayName = "Tenant A" };
                await repository.AddAsync(secretA);

                await Assert.That(secretA.TenantId).IsEqualTo("tenant-a");
                var loadedA = await repository.GetAsync("smtp:password");
                await Assert.That(loadedA!.DisplayName).IsEqualTo("Tenant A");
                await Assert.That(loadedA.TenantId).IsEqualTo("tenant-a");
                await Assert.That(await repository.ListAsync()).HasSingleItem();

                await Assert.That(() => repository.AddAsync(new Secret
                {
                    Name = "smtp:password",
                    DisplayName = "Duplicate"
                })).ThrowsExactly<InvalidOperationException>();
            }

            using (UseTenant(tenantAccessor, "tenant-b"))
            {
                var secretB = new Secret { Name = "smtp:password", DisplayName = "Tenant B" };
                await repository.AddAsync(secretB);

                await Assert.That(secretB.TenantId).IsEqualTo("tenant-b");
                await Assert.That((await repository.GetAsync("SMTP:PASSWORD"))!.DisplayName).IsEqualTo("Tenant B");
                await Assert.That(await repository.ListAsync()).HasSingleItem();
            }

            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                await Assert.That((await repository.GetAsync("smtp:password"))!.DisplayName).IsEqualTo("Tenant A");
                await Assert.That(await repository.ListAsync()).HasSingleItem();
            }
        });
    }

    [Test]
    public async Task Repositories_SaveDoesNotOverwriteAnotherTenantsSecret()
    {
        await ForEachRepositoryAsync(async (tenantAccessor, repository) =>
        {
            using (UseTenant(tenantAccessor, "tenant-a"))
                await repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "Tenant A" });

            using (UseTenant(tenantAccessor, "tenant-b"))
                await repository.SaveAsync(new Secret { Name = "SMTP:PASSWORD", DisplayName = "Tenant B" });

            using (UseTenant(tenantAccessor, "tenant-a"))
                await Assert.That((await repository.GetAsync("smtp:password"))!.DisplayName).IsEqualTo("Tenant A");

            using (UseTenant(tenantAccessor, "tenant-b"))
                await Assert.That((await repository.GetAsync("smtp:password"))!.DisplayName).IsEqualTo("Tenant B");
        });
    }

    [Test]
    public async Task Repositories_RejectExplicitHiddenTenantDuplicatesWithoutMutation()
    {
        await ForEachRepositoryAsync(async (tenantAccessor, repository) =>
        {
            using (UseTenant(tenantAccessor, "tenant-b"))
                await repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "Tenant B", TenantId = "tenant-b" });

            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                await Assert.That(() => repository.AddAsync(new Secret
                {
                    Name = "SMTP:PASSWORD",
                    DisplayName = "Duplicate",
                    TenantId = "tenant-b"
                })).ThrowsExactly<InvalidOperationException>();
                await Assert.That(() => repository.SaveAsync(new Secret
                {
                    Name = "smtp:password",
                    DisplayName = "Duplicate",
                    TenantId = "tenant-b"
                })).ThrowsExactly<InvalidOperationException>();
                await Assert.That(await repository.TryAddOrReplaceDeletedAsync(new Secret
                {
                    Name = "smtp:password",
                    DisplayName = "Duplicate",
                    TenantId = "tenant-b"
                })).IsFalse();
            }

            using (UseTenant(tenantAccessor, "tenant-b"))
                await Assert.That((await repository.GetAsync("smtp:password"))!.DisplayName).IsEqualTo("Tenant B");
        });
    }

    [Test]
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
                await Assert.That(updated!.DisplayName).IsEqualTo("Updated A");
                await Assert.That(updated.TenantId).IsEqualTo("tenant-a");
            }

            using (UseTenant(tenantAccessor, "tenant-b"))
                await Assert.That(await repository.GetAsync("smtp:password")).IsNull();
        });
    }

    [Test]
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

                await Assert.That(await repository.TryAddOrReplaceDeletedAsync(new Secret
                {
                    Name = "SMTP:PASSWORD",
                    DisplayName = "Replacement A"
                })).IsTrue();
                await Assert.That((await repository.GetAsync("smtp:password"))!.DisplayName).IsEqualTo("Replacement A");
            }

            using (UseTenant(tenantAccessor, "tenant-b"))
            {
                // Tenant B has no visibility into A's deleted row and can create its own row instead.
                await Assert.That(await repository.TryAddOrReplaceDeletedAsync(new Secret
                {
                    Name = "smtp:password",
                    DisplayName = "Tenant B"
                })).IsTrue();
                await Assert.That((await repository.GetAsync("smtp:password"))!.DisplayName).IsEqualTo("Tenant B");
            }
        });
    }

    [Test]
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
                await Assert.That((await repository.GetAsync("SHARED:SECRET"))!.DisplayName).IsEqualTo("Shared");
                await Assert.That(() => repository.SaveAsync(new Secret
                {
                    Name = "shared:secret",
                    DisplayName = "Rehomed"
                })).ThrowsExactly<InvalidOperationException>();
                await Assert.That(await repository.TryAddOrReplaceDeletedAsync(new Secret
                {
                    Name = "shared:secret",
                    DisplayName = "Rehomed"
                })).IsFalse();
            }

            using (UseTenant(tenantAccessor, "tenant-b"))
                await Assert.That((await repository.GetAsync("shared:secret"))!.DisplayName).IsEqualTo("Shared");
        });
    }

    [Test]
    public async Task Repositories_PreserveNullRowsForLegacyNoAccessorUse()
    {
        var inMemory = new InMemorySecretRepository();
        await inMemory.AddAsync(new Secret { Name = "legacy:secret", DisplayName = "Legacy" });
        var loadedInMemory = await inMemory.GetAsync("legacy:secret");
        await Assert.That(loadedInMemory!.TenantId).IsNull();

        var path = Path.Join(Path.GetTempPath(), $"elsa-secrets-{Guid.NewGuid():N}.json");
        try
        {
            var file = new FileSecretRepository(Microsoft.Extensions.Options.Options.Create(new SecretsOptions { RepositoryFilePath = path }));
            await file.AddAsync(new Secret { Name = "legacy:secret", DisplayName = "Legacy" });
            var reloaded = new FileSecretRepository(Microsoft.Extensions.Options.Options.Create(new SecretsOptions { RepositoryFilePath = path }));

            var loadedFile = await reloaded.GetAsync("legacy:secret");
            await Assert.That(loadedFile!.TenantId).IsNull();
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
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

    [Test]
    public async Task Repositories_NormalizeNamesForLookupAndUniqueness()
    {
        await ForEachRepositoryAsync(async (tenantAccessor, repository) =>
        {
            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                await repository.AddAsync(new Secret { Name = " smtp:password ", DisplayName = "Original" });
                await Assert.That((await repository.GetAsync(" SMTP:PASSWORD "))!.DisplayName).IsEqualTo("Original");

                await Assert.That(() => repository.AddAsync(new Secret
                {
                    Name = "SMTP:PASSWORD",
                    DisplayName = "Duplicate"
                })).ThrowsExactly<InvalidOperationException>();

                await repository.SaveAsync(new Secret { Name = " SMTP:PASSWORD ", DisplayName = "Updated" });
                await Assert.That((await repository.GetAsync("smtp:password"))!.DisplayName).IsEqualTo("Updated");
                await Assert.That(await repository.TryAddOrReplaceDeletedAsync(new Secret
                {
                    Name = " smtp:password ",
                    DisplayName = "Active duplicate"
                })).IsFalse();
            }

            using (UseTenant(tenantAccessor, "tenant-b"))
            {
                await repository.AddAsync(new Secret { Name = "SMTP:PASSWORD", DisplayName = "Tenant B" });
                await Assert.That((await repository.GetAsync(" smtp:password "))!.DisplayName).IsEqualTo("Tenant B");
            }
        });
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
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
                    await Assert.That(await repository.GetAsync("smtp:password")).IsNull();
                    await repository.AddAsync(new Secret { Name = "SMTP:PASSWORD", DisplayName = "Tenant B" });
                }
                else
                {
                    await Assert.That((await repository.GetAsync("smtp:password"))!.DisplayName).IsEqualTo("Tenant A");
                    await Assert.That(() => repository.AddAsync(new Secret
                    {
                        Name = "SMTP:PASSWORD",
                        DisplayName = "Duplicate"
                    })).ThrowsExactly<InvalidOperationException>();

                    await repository.AddAsync(new Secret
                    {
                        Name = "explicit:secret",
                        DisplayName = "Explicit Tenant A",
                        TenantId = "tenant-a"
                    });

                    await Assert.That((await repository.GetAsync("EXPLICIT:SECRET"))!.DisplayName).IsEqualTo("Explicit Tenant A");
                    await Assert.That(await repository.ListAsync()).Contains(x => x.Name == "explicit:secret");
                    await Assert.That(() => repository.AddAsync(new Secret
                    {
                        Name = " explicit:secret ",
                        DisplayName = "Duplicate Explicit",
                        TenantId = "tenant-b"
                    })).ThrowsExactly<InvalidOperationException>();
                }
            }
        });
    }

    [Test]
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

            var saved = await Assert.That(await repository.GetAsync("save:secret")).IsNotNull();
            await Assert.That(saved.Id).IsEqualTo("save-existing");
            await Assert.That(saved.TenantId).IsEqualTo("tenant-b");

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

            await Assert.That(replaced).IsTrue();
            var replacement = await Assert.That(await repository.GetAsync("replace:secret")).IsNotNull();
            await Assert.That(replacement.Id).IsEqualTo("replace-incoming");
            await Assert.That(replacement.TenantId).IsEqualTo("tenant-b");
        });
    }

    [Test]
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
                await Assert.That(await namedRepository.ListAsync()).IsEmpty();

            var defaultAccessor = new DefaultTenantAccessor();
            var defaultRepository = new FileSecretRepository(options, null, defaultAccessor);
            await Assert.That((await defaultRepository.GetAsync("legacy:secret"))!.DisplayName).IsEqualTo("Legacy");
            await Assert.That((await defaultRepository.GetAsync("legacy:secret"))!.TenantId).IsNull();
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

        await Assert.That(() => repository.AddAsync(explicitDefault)).ThrowsExactly<InvalidOperationException>();
        await Assert.That(() => repository.SaveAsync(explicitDefault)).ThrowsExactly<InvalidOperationException>();
        await Assert.That(await repository.TryAddOrReplaceDeletedAsync(explicitDefault)).IsFalse();
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
