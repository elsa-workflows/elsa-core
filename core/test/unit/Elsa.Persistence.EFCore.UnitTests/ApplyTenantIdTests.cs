using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Tenants;
using Elsa.Persistence.EFCore.Sqlite;
using Elsa.Tenants.Options;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// Verifies the host ownership of tenant registry rows.
/// </summary>
public class ApplyTenantIdTests
{
    [Fact]
    public async Task TenantRegistryRowsStayHostScopedWhenAccessedFromATenant()
    {
        var accessor = new DefaultTenantAccessor();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ITenantAccessor>(accessor)
            .Configure<TenantsOptions>(options => options.IsEnabled = true)
            .AddScoped<IEntitySavingHandler, ApplyTenantId>()
            .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
            .AddSqliteEntityModelCreatingHandlers()
            .AddDbContextFactory<TenantsElsaDbContext>((_, builder) => builder.UseSqlite(connection))
            .Decorate<IDbContextFactory<TenantsElsaDbContext>, TenantAwareDbContextFactory<TenantsElsaDbContext>>()
            .BuildServiceProvider();
        var factory = services.GetRequiredService<IDbContextFactory<TenantsElsaDbContext>>();
        var store = new EFCoreTenantStore(new EntityStore<TenantsElsaDbContext, Tenant>(factory, services));
        using var scope = accessor.PushContext(new Tenant { Id = "tenant-a", Name = "Tenant A" });
        await using (var database = await factory.CreateDbContextAsync())
            await database.Database.EnsureCreatedAsync();

        await store.AddAsync(new Tenant { Id = Tenant.DefaultTenantId, Name = "Default" });
        await store.AddAsync(new Tenant { Id = "tenant-b", Name = "Tenant B", TenantId = "legacy-owner" });

        var tenants = (await store.ListAsync()).OrderBy(tenant => tenant.Id).ToArray();
        Assert.Equal(new[] { "", "tenant-b" }, tenants.Select(tenant => tenant.Id));
        Assert.Null(tenants[0].TenantId);
        Assert.Equal("legacy-owner", tenants[1].TenantId);
    }

}
