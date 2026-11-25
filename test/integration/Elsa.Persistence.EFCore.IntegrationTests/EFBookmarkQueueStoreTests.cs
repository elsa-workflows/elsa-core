using System.Data.Common;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Persistence.EFCore.EntityHandlers;
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
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Persistence.EFCore.IntegrationTests;

public class EFBookmarkQueueStoreTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly QueryObserver _observer = new();
    private readonly ServiceProvider _services;
    private readonly ContextFactory _factory;
    private readonly EFBookmarkQueueStore _store;

    /// <summary>
    /// Creates an isolated SQLite-backed store with Elsa's tenant query filter enabled.
    /// </summary>
    public EFBookmarkQueueStoreTests()
    {
        _services = new ServiceCollection()
            .AddSingleton<IEntityModelCreatingHandler>(new SetTenantIdFilter(Options.Create(new TenantsOptions { IsEnabled = true })))
            .AddSingleton<IEntityModelCreatingHandler, SetupForSqlite>()
            .BuildServiceProvider();
        var options = new DbContextOptionsBuilder<RuntimeElsaDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(_observer)
            .Options;
        _factory = new ContextFactory(options, _services);
        _store = new EFBookmarkQueueStore(new Store<RuntimeElsaDbContext, BookmarkQueueItem>(_factory, _services), Substitute.For<IPayloadSerializer>());
    }

    /// <summary>
    /// Seeds matching and non-matching workflows across two tenants before observing page queries.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await using var context = _factory.CreateDbContext();
        await context.Database.EnsureCreatedAsync();
        context.BookmarkQueueItems.AddRange(Enumerable.Range(0, 160).Select(i => new BookmarkQueueItem
        {
            Id = $"item-{i:D3}",
            TenantId = i < 120 ? "tenant-a" : "tenant-b",
            WorkflowInstanceId = i < 60 || i >= 120 ? "selected" : "other",
            CreatedAt = DateTimeOffset.UnixEpoch
        }));
        await context.SaveChangesAsync();
        _observer.Commands.Clear();
        _observer.MaterializedItems = 0;
    }

    /// <summary>
    /// Verifies totals, page boundaries and tenant isolation while counting without materializing matching rows.
    /// </summary>
    [Theory]
    [InlineData(null, 0, 120, 50)]
    [InlineData(null, 50, 120, 50)]
    [InlineData(null, 150, 120, 0)]
    [InlineData("selected", 0, 60, 50)]
    [InlineData("selected", 50, 60, 10)]
    [InlineData("selected", 100, 60, 0)]
    [InlineData("missing", 0, 0, 0)]
    public async Task PageAsync_CountsInDatabase_AndMaterializesOnlyTheRequestedPage(string? workflowInstanceId, int offset, long totalCount, int pageSize)
    {
        var pageArgs = PageArgs.FromRange(offset, 50);
        var order = new BookmarkQueueItemOrder<string>(x => x.Id, OrderDirection.Ascending);

        var page = workflowInstanceId == null
            ? await _store.PageAsync(pageArgs, order)
            : await _store.PageAsync(pageArgs, new BookmarkQueueFilter { WorkflowInstanceId = workflowInstanceId }, order);

        Assert.Equal(totalCount, page.TotalCount);
        Assert.Equal(Enumerable.Range(offset, pageSize).Select(i => $"item-{i:D3}"), page.Items.Select(x => x.Id));
        Assert.All(page.Items, x => Assert.Equal("tenant-a", x.TenantId));
        // An in-memory count returns the same total but materializes every matching row first.
        Assert.Equal(pageSize, _observer.MaterializedItems);
        Assert.Equal(2, _observer.Commands.Count);
        Assert.Contains("COUNT(*)", _observer.Commands[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT", _observer.Commands[1], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OFFSET", _observer.Commands[1], StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Orders timestamp ties by ID across SQL pages while preserving primary direction, filters and tenant isolation.
    /// </summary>
    [Theory]
    [InlineData(false, OrderDirection.Ascending)]
    [InlineData(false, OrderDirection.Descending)]
    [InlineData(true, OrderDirection.Ascending)]
    [InlineData(true, OrderDirection.Descending)]
    public async Task PageAsync_BreaksTimestampTiesById(bool filtered, OrderDirection direction)
    {
        await SeedTiedItemsAsync(false);
        var candidates = Enumerable.Range(0, 120).Where(i => !filtered || i % 2 == 0);
        var ordered = direction == OrderDirection.Ascending
            ? candidates.OrderBy(i => i / 40).ThenBy(i => i)
            : candidates.OrderByDescending(i => i / 40).ThenBy(i => i);
        var expected = ordered.Select(i => $"item-{i:D3}").ToArray();
        var observed = new List<string>();

        for (var offset = 0; offset < expected.Length; offset += 50)
        {
            var pageArgs = PageArgs.FromRange(offset, 50);
            var order = new BookmarkQueueItemOrder<DateTimeOffset>(x => x.CreatedAt, direction);
            var page = filtered
                ? await _store.PageAsync(pageArgs, new BookmarkQueueFilter { WorkflowInstanceId = "selected" }, order)
                : await _store.PageAsync(pageArgs, order);

            Assert.Equal(expected.Length, page.TotalCount);
            Assert.All(page.Items, x => Assert.Equal("tenant-a", x.TenantId));
            observed.AddRange(page.Items.Select(x => x.Id));
        }

        Assert.Equal(expected, observed);
    }

    /// <summary>
    /// Attempts tied stimuli once per pass through the real EF store, including failures on page boundaries.
    /// </summary>
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(-1, 1)]
    [InlineData(-1, 3)]
    [InlineData(49, 3)]
    [InlineData(50, 3)]
    public async Task ProcessAsync_AttemptsTiedItemsOnce_InStableOrder(int failedId, int resumeEvery)
    {
        await SeedTiedItemsAsync(true);
        var attempted = new List<int>();
        var resumer = Substitute.For<IWorkflowResumer>();
        resumer.ResumeAsync(Arg.Any<BookmarkFilter>(), Arg.Any<ResumeBookmarkOptions?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var id = int.Parse(call.Arg<BookmarkFilter>().BookmarkId!);
                attempted.Add(id);
                return id == failedId
                    ? Task.FromException<IEnumerable<RunWorkflowInstanceResponse>>(new InvalidOperationException("Controlled resume failure"))
                    : Task.FromResult<IEnumerable<RunWorkflowInstanceResponse>>(resumeEvery > 0 && id % resumeEvery == 0 ? [new()] : []);
            });
        var processor = new BookmarkQueueProcessor(_store, resumer, NullLogger<BookmarkQueueProcessor>.Instance);

        await processor.ProcessAsync();

        Assert.Equal(Enumerable.Range(0, 120), attempted);
        var expectedRetained = Enumerable.Range(0, 120).Where(i => i == failedId || resumeEvery == 0 || i % resumeEvery != 0).ToArray();
        var remaining = await _store.FindManyAsync(new BookmarkQueueFilter());
        Assert.Equal(expectedRetained, remaining.Select(x => int.Parse(x.BookmarkId!)).Order());

        attempted.Clear();
        await processor.ProcessAsync();

        Assert.Equal(expectedRetained, attempted);
        remaining = await _store.FindManyAsync(new BookmarkQueueFilter());
        Assert.Equal(expectedRetained, remaining.Select(x => int.Parse(x.BookmarkId!)).Order());
        await using var context = _factory.CreateDbContext();
        Assert.Equal(40, await context.BookmarkQueueItems.IgnoreQueryFilters().CountAsync(x => x.TenantId == "tenant-b"));
    }

    /// <summary>
    /// Inserts stimuli individually in reverse ID order so database insertion order cannot satisfy the tie-breaker accidentally.
    /// </summary>
    private async Task SeedTiedItemsAsync(bool singleTimestamp)
    {
        await using var context = _factory.CreateDbContext();
        await context.BookmarkQueueItems.ExecuteDeleteAsync();
        for (var i = 119; i >= 0; i--)
        {
            await _store.AddAsync(new BookmarkQueueItem
            {
                Id = $"item-{i:D3}",
                BookmarkId = i.ToString(),
                TenantId = "tenant-a",
                WorkflowInstanceId = i % 2 == 0 ? "selected" : "other",
                CreatedAt = DateTimeOffset.UnixEpoch.AddSeconds(singleTimestamp ? 0 : i / 40)
            });
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _services.DisposeAsync();
    }

    private sealed class ContextFactory(DbContextOptions<RuntimeElsaDbContext> options, IServiceProvider services) : IDbContextFactory<RuntimeElsaDbContext>
    {
        /// <summary>
        /// Creates a context scoped to the tenant whose queue is being paged.
        /// </summary>
        public RuntimeElsaDbContext CreateDbContext() => new(options, services) { TenantId = "tenant-a" };
    }

    private sealed class QueryObserver : DbCommandInterceptor, IMaterializationInterceptor
    {
        public List<string> Commands { get; } = [];
        public int MaterializedItems { get; set; }

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        /// <summary>
        /// Counts bookmark queue entities materialized by the observed queries.
        /// </summary>
        public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
        {
            if (entity is BookmarkQueueItem)
            {
                MaterializedItems++;
            }

            return entity;
        }
    }
}
