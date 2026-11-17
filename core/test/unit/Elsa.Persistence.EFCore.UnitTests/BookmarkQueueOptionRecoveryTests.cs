using Elsa.Common;
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
using Elsa.Workflows.Serialization.Serializers;
using Elsa.Workflows.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Persistence.EFCore.UnitTests;

public class BookmarkQueueOptionRecoveryTests
{
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

        private Fixture(string databasePath, ServiceProvider services, IServiceScope scope, RuntimeElsaDbContext dbContext)
        {
            _databasePath = databasePath;
            _services = services;
            _scope = scope;
            DbContext = dbContext;
            var serializer = new JsonPayloadSerializer(scope.ServiceProvider);
            QueueStore = new EFBookmarkQueueStore(scope.ServiceProvider.GetRequiredService<Store<RuntimeElsaDbContext, BookmarkQueueItem>>(), serializer);
            DeadLetterStore = new EFBookmarkQueueDeadLetterStore(scope.ServiceProvider.GetRequiredService<Store<RuntimeElsaDbContext, BookmarkQueueDeadLetterItem>>(), serializer);
        }

        public RuntimeElsaDbContext DbContext { get; }
        public EFBookmarkQueueStore QueueStore { get; }
        public EFBookmarkQueueDeadLetterStore DeadLetterStore { get; }

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
            return new(databasePath, services, scope, dbContext);
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
