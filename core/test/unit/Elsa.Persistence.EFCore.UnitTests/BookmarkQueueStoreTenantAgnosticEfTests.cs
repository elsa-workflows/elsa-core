using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Persistence.EFCore.Sqlite;
using Elsa.Tenants.Options;
using Elsa.Workflows;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Persistence.EFCore.UnitTests;

public class BookmarkQueueStoreTenantAgnosticEfTests
{
    private static readonly Dictionary<string, string> TenantById = new()
    {
        ["q-t1"] = "t1",
        ["q-t2"] = "t2",
        ["q-default"] = Tenant.DefaultTenantId
    };

    [Theory(DisplayName = "Page honors TenantAgnostic for the items and the total count")]
    [InlineData(true, new[] { "q-default", "q-t1", "q-t2" })]
    [InlineData(false, new[] { "q-t1" })]
    public async Task PageFollowsTenantAgnostic(bool tenantAgnostic, string[] expectedIds)
    {
        await using var fixture = await Fixture.CreateAsync();

        foreach (var (id, tenantId) in TenantById)
        {
            fixture.DbContext.BookmarkQueueItems.Add(new() { Id = id, TenantId = tenantId, CreatedAt = DateTimeOffset.UnixEpoch });
            fixture.DbContext.BookmarkQueueDeadLetterItems.Add(new() { Id = id, TenantId = tenantId, OriginalQueueItemId = id, Reason = "test" });
        }

        await fixture.DbContext.SaveChangesAsync();

        using var _ = fixture.PushTenant("t1");
        var pageArgs = PageArgs.All;
        var page = await fixture.QueueStore.PageAsync(pageArgs, new BookmarkQueueFilter { TenantAgnostic = tenantAgnostic }, new BookmarkQueueItemOrder<string>(x => x.Id, OrderDirection.Ascending));
        var deadLetterPage = await fixture.DeadLetterStore.PageAsync(pageArgs, new BookmarkQueueDeadLetterFilter { TenantAgnostic = tenantAgnostic }, new BookmarkQueueDeadLetterItemOrder<string>(x => x.Id, OrderDirection.Ascending));

        Assert.Equal(expectedIds, page.Items.Select(x => x.Id));
        Assert.Equal(expectedIds.Length, page.TotalCount);
        Assert.Equal(expectedIds, deadLetterPage.Items.Select(x => x.Id));
        Assert.Equal(expectedIds.Length, deadLetterPage.TotalCount);
    }

    [Theory(DisplayName = "Paged FindManyAsync honors TenantAgnostic, ordering and paging without a total")]
    [InlineData(true, new[] { "q-t1", "q-t2" })]
    [InlineData(false, new string[0])]
    public async Task FindManyPagedFollowsTenantAgnostic(bool tenantAgnostic, string[] expectedIds)
    {
        await using var fixture = await Fixture.CreateAsync();

        foreach (var (id, tenantId) in TenantById)
        {
            fixture.DbContext.BookmarkQueueItems.Add(new() { Id = id, TenantId = tenantId, CreatedAt = DateTimeOffset.UnixEpoch });
            fixture.DbContext.BookmarkQueueDeadLetterItems.Add(new() { Id = id, TenantId = tenantId, OriginalQueueItemId = id, Reason = "test" });
        }

        await fixture.DbContext.SaveChangesAsync();

        using var _ = fixture.PushTenant("t1");
        var pageArgs = PageArgs.FromRange(1, 2);
        var items = await fixture.QueueStore.FindManyAsync(new BookmarkQueueFilter { TenantAgnostic = tenantAgnostic }, new BookmarkQueueItemOrder<string>(x => x.Id, OrderDirection.Ascending), pageArgs);
        var deadLetterItems = await fixture.DeadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter { TenantAgnostic = tenantAgnostic }, new BookmarkQueueDeadLetterItemOrder<string>(x => x.Id, OrderDirection.Ascending), pageArgs);

        Assert.Equal(expectedIds, items.Select(x => x.Id));
        Assert.Equal(expectedIds, deadLetterItems.Select(x => x.Id));
    }

    [Fact(DisplayName = "ListTenantIdsAsync returns the distinct normalized tenant ids across tenants")]
    public async Task ListTenantIdsAsync_ReturnsDistinctNormalizedTenantIds()
    {
        await using var fixture = await Fixture.CreateAsync();

        fixture.DbContext.BookmarkQueueItems.AddRange(
            new BookmarkQueueItem { Id = "a", TenantId = "t1" },
            new BookmarkQueueItem { Id = "b", TenantId = "t1" },
            new BookmarkQueueItem { Id = "c", TenantId = "t2" },
            new BookmarkQueueItem { Id = "d", TenantId = Tenant.DefaultTenantId },
            new BookmarkQueueItem { Id = "e", TenantId = null });

        await fixture.DbContext.SaveChangesAsync();

        var tenantIds = await fixture.QueueStore.ListTenantIdsAsync();

        Assert.NotNull(tenantIds);
        Assert.Equal(new[] { "", "t1", "t2" }, tenantIds.Order());
    }

    [Fact(DisplayName = "DeleteAsync stays tenant-scoped even when TenantAgnostic is set")]
    public async Task DeleteAsync_StaysTenantScoped()
    {
        await using var fixture = await Fixture.CreateAsync();

        fixture.DbContext.BookmarkQueueItems.AddRange(
            new BookmarkQueueItem { Id = "q-t1", TenantId = "t1" },
            new BookmarkQueueItem { Id = "q-t2", TenantId = "t2" });

        await fixture.DbContext.SaveChangesAsync();

        using var _ = fixture.PushTenant("t1");
        var deleted = await fixture.QueueStore.DeleteAsync(new BookmarkQueueFilter { TenantAgnostic = true, Ids = ["q-t1", "q-t2"] });

        Assert.Equal(1, deleted);
        var remaining = await fixture.QueueStore.FindManyAsync(new() { TenantAgnostic = true });
        Assert.Equal(["q-t2"], remaining.Select(x => x.Id));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string _databasePath;
        private readonly ServiceProvider _services;
        private readonly IServiceScope _scope;
        private readonly DefaultTenantAccessor _tenantAccessor;

        private Fixture(string databasePath, ServiceProvider services, IServiceScope scope, DefaultTenantAccessor tenantAccessor, RuntimeElsaDbContext dbContext)
        {
            _databasePath = databasePath;
            _services = services;
            _scope = scope;
            _tenantAccessor = tenantAccessor;
            DbContext = dbContext;
            var serializer = Substitute.For<IPayloadSerializer>();
            QueueStore = new EFBookmarkQueueStore(scope.ServiceProvider.GetRequiredService<Store<RuntimeElsaDbContext, BookmarkQueueItem>>(), serializer);
            DeadLetterStore = new EFBookmarkQueueDeadLetterStore(scope.ServiceProvider.GetRequiredService<Store<RuntimeElsaDbContext, BookmarkQueueDeadLetterItem>>(), serializer);
        }

        public RuntimeElsaDbContext DbContext { get; }
        public EFBookmarkQueueStore QueueStore { get; }
        public EFBookmarkQueueDeadLetterStore DeadLetterStore { get; }

        public IDisposable PushTenant(string tenantId) => _tenantAccessor.PushContext(new Tenant { Id = tenantId, Name = tenantId });

        public static async Task<Fixture> CreateAsync()
        {
            var databasePath = Path.Join(Path.GetTempPath(), $"elsa-bookmark-queue-{Guid.NewGuid():N}.db");
            var tenantAccessor = new DefaultTenantAccessor();
            var services = new ServiceCollection()
                .AddLogging()
                .AddSingleton<ITenantAccessor>(tenantAccessor)
                .Configure<TenantsOptions>(options => options.IsEnabled = true)
                .AddScoped<IEntitySavingHandler, ApplyTenantId>()
                .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
                .AddSqliteEntityModelCreatingHandlers()
                .AddDbContextFactory<RuntimeElsaDbContext>((_, builder) => builder.UseElsaSqlite(typeof(RuntimeDbContextFactory).Assembly, $"Data Source={databasePath};Default Timeout=30"))
                .Decorate<IDbContextFactory<RuntimeElsaDbContext>, TenantAwareDbContextFactory<RuntimeElsaDbContext>>()
                .AddScoped<Store<RuntimeElsaDbContext, BookmarkQueueItem>>()
                .AddScoped<Store<RuntimeElsaDbContext, BookmarkQueueDeadLetterItem>>()
                .BuildServiceProvider();

            var dbContext = await services.GetRequiredService<IDbContextFactory<RuntimeElsaDbContext>>().CreateDbContextAsync();
            await dbContext.Database.EnsureCreatedAsync();

            var scope = services.CreateScope();
            return new(databasePath, services, scope, tenantAccessor, dbContext);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            _scope.Dispose();
            await _services.DisposeAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(_databasePath);
        }
    }
}
