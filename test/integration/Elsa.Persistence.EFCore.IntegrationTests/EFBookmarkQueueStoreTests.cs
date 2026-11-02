using System.Data.Common;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Tenants.Options;
using Elsa.Workflows;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
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
