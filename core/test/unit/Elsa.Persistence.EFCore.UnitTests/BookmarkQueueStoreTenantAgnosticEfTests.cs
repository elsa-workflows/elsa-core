using Elsa.Common;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Common.Serialization;
using Elsa.Common.Services;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Persistence.EFCore.Sqlite;
using Elsa.Tenants.Options;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.Serialization.Serializers;
using Elsa.Workflows.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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

    [Fact]
    public async Task QueuePages_OrderEqualTimestampsById()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.DbContext.BookmarkQueueItems.AddRange(
            new BookmarkQueueItem { Id = "c", TenantId = "t1", CreatedAt = DateTimeOffset.UnixEpoch },
            new BookmarkQueueItem { Id = "b", TenantId = "t1", CreatedAt = DateTimeOffset.UnixEpoch },
            new BookmarkQueueItem { Id = "a", TenantId = "t2", CreatedAt = DateTimeOffset.UnixEpoch });
        await fixture.DbContext.SaveChangesAsync();

        using var _ = fixture.PushTenant("t1");
        var order = new BookmarkQueueItemOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Descending);
        var filter = new BookmarkQueueFilter { TenantAgnostic = true };
        var firstPage = PageArgs.FromRange(0, 1);
        var secondPage = PageArgs.FromRange(1, 1);

        Assert.Equal("a", Assert.Single(await fixture.QueueStore.FindManyAsync(filter, order, firstPage)).Id);
        Assert.Equal("b", Assert.Single(await fixture.QueueStore.FindManyAsync(filter, order, secondPage)).Id);
        Assert.Equal("b", Assert.Single((await fixture.QueueStore.PageAsync(secondPage, filter, order)).Items).Id);
        Assert.Equal("b", Assert.Single((await fixture.QueueStore.PageAsync(firstPage, order)).Items).Id);
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

    [Fact]
    public async Task ProcessorQuarantinesUnreadableOptionsAndResumesTheNextItem()
    {
        await using var fixture = await Fixture.CreateAsync();
        const string unreadableOptions = """{"input":""";
        var unreadable = new BookmarkQueueItem
        {
            Id = "unreadable",
            CreatedAt = DateTimeOffset.UnixEpoch,
            BookmarkId = "first"
        };
        var valid = new BookmarkQueueItem
        {
            Id = "valid",
            CreatedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
            BookmarkId = "second"
        };
        fixture.DbContext.BookmarkQueueItems.AddRange(unreadable, valid);
        fixture.DbContext.Entry(unreadable).Property("SerializedOptions").CurrentValue = unreadableOptions;
        fixture.DbContext.Entry(valid).Property("SerializedOptions").CurrentValue = """{"input":{"name":"Alice"}}""";
        await fixture.DbContext.SaveChangesAsync();

        var resumer = Substitute.For<IWorkflowResumer>();
        resumer.ResumeAsync(Arg.Any<BookmarkFilter>(), Arg.Any<ResumeBookmarkOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<RunWorkflowInstanceResponse>>([new()]));
        var clock = new SystemClock();
        var deadLetters = new BookmarkQueueDeadLetterManager(fixture.DeadLetterStore, fixture.QueueStore,
            Substitute.For<IBookmarkQueueSignaler>(), clock, new GuidIdentityGenerator(), NullLogger<BookmarkQueueDeadLetterManager>.Instance);
        var processor = new BookmarkQueueProcessor(fixture.QueueStore, deadLetters, resumer, clock,
            Options.Create(new BookmarkQueuePurgeOptions()), NullLogger<BookmarkQueueProcessor>.Instance);

        await processor.ProcessAsync();

        await resumer.Received(1).ResumeAsync(Arg.Is<BookmarkFilter>(filter => filter.BookmarkId == "second"),
            Arg.Is<ResumeBookmarkOptions>(options => options.Input!["name"].Equals("Alice")), Arg.Any<CancellationToken>());
        await resumer.DidNotReceive().ResumeAsync(Arg.Is<BookmarkFilter>(filter => filter.BookmarkId == "first"),
            Arg.Any<ResumeBookmarkOptions?>(), Arg.Any<CancellationToken>());
        Assert.Empty(await fixture.QueueStore.FindManyAsync(new()));
        var deadLetter = Assert.Single(await fixture.DeadLetterStore.FindManyAsync(new()));
        Assert.Equal(unreadable.Id, deadLetter.OriginalQueueItemId);
        Assert.Equal("UnreadableOptions", deadLetter.Reason);
        Assert.False(deadLetter.CanReplay);
        Assert.False((await deadLetters.ReplayAsync(deadLetter.Id)).Succeeded);
        Assert.Equal(unreadableOptions, await fixture.DbContext.BookmarkQueueDeadLetterItems
            .Select(item => EF.Property<string>(item, "SerializedOptions")).SingleAsync());
    }

    [Fact]
    public async Task LegacyDeadLetterRetainsItsInputWhenClaimedForReplay()
    {
        await using var fixture = await Fixture.CreateAsync();
        var deadLetter = new BookmarkQueueDeadLetterItem
        {
            Id = "old",
            OriginalQueueItemId = "old-queue-item",
            Reason = "Failed"
        };
        fixture.DbContext.BookmarkQueueDeadLetterItems.Add(deadLetter);
        const string legacyOptions = """{"input":{"names":{"_type":"System.Collections.Generic.List`1[[System.String, System.Private.CoreLib]], System.Private.CoreLib","_items":["Alice"]}}}""";
        fixture.DbContext.Entry(deadLetter).Property("SerializedOptions").CurrentValue = legacyOptions;
        await fixture.DbContext.SaveChangesAsync();

        var replayed = await fixture.DeadLetterStore.TryMarkReplayedAsync(deadLetter.Id, "replacement", DateTimeOffset.UnixEpoch);

        Assert.NotNull(replayed);
        Assert.Equal(["Alice"], Assert.IsType<List<string>>(replayed.Options!.Input!["names"]));
        Assert.Null(await fixture.DeadLetterStore.TryMarkReplayedAsync(deadLetter.Id, "another", DateTimeOffset.UnixEpoch));
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
            var serializer = new JsonPayloadSerializer(scope.ServiceProvider);
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
                .AddSingleton(SerializationTypeRegistry.CreateDefault())
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
