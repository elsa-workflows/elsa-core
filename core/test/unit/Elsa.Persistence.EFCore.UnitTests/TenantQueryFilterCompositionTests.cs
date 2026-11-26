using Elsa.Common.Entities;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Tenants.Options;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.UnitTests;

public class TenantQueryFilterCompositionTests
{
    [Theory]
    [InlineData(true, "tenant-a", "a-active", "shared-active")]
    [InlineData(true, "tenant-b", "b-active", "shared-active")]
    [InlineData(true, "", "default-active", "null-active", "shared-active")]
    [InlineData(false, "tenant-a", "a-active", "b-active", "default-active", "null-active", "shared-active")]
    public async Task TenantFilter_PreservesExistingFiltersAndContextVisibility(bool filteringEnabled, string tenantId, params string[] expectedIds)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.Configure<TenantsOptions>(options => options.IsEnabled = filteringEnabled);
        services.AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>();
        await using var serviceProvider = services.BuildServiceProvider();
        var options = new DbContextOptionsBuilder<FilteredDbContext>().UseSqlite(connection).Options;
        await using var context = new FilteredDbContext(options, serviceProvider) { TenantId = tenantId };
        await context.Database.EnsureCreatedAsync();
        context.Set<FilteredEntity>().AddRange(
            new FilteredEntity { Id = "a-active", TenantId = "tenant-a" },
            new FilteredEntity { Id = "a-deleted", TenantId = "tenant-a", IsDeleted = true },
            new FilteredEntity { Id = "a-hidden", TenantId = "tenant-a", IsHidden = true },
            new FilteredEntity { Id = "b-active", TenantId = "tenant-b" },
            new FilteredEntity { Id = "default-active", TenantId = "" },
            new FilteredEntity { Id = "null-active", TenantId = null },
            new FilteredEntity { Id = "shared-active", TenantId = "*" });
        await context.SaveChangesAsync();

        var visibleIds = await context.Set<FilteredEntity>().OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync();

        Assert.Equal(expectedIds, visibleIds);
    }

    private sealed class FilteredDbContext(DbContextOptions<FilteredDbContext> options, IServiceProvider serviceProvider)
        : ElsaDbContextBase(options, serviceProvider)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<FilteredEntity>();
            entity.HasKey(x => x.Id);
            entity.HasQueryFilter(x => !x.IsDeleted && !x.IsHidden);
            base.OnModelCreating(modelBuilder);
        }
    }

    private sealed class FilteredEntity : Entity
    {
        public bool IsDeleted { get; set; }
        public bool IsHidden { get; set; }
    }
}
