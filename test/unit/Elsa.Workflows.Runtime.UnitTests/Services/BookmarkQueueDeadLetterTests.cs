using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Stores;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class BookmarkQueueDeadLetterTests
{
    private readonly DateTimeOffset _now = new(2026, 5, 20, 10, 0, 0, TimeSpan.Zero);
    private readonly TestClock _clock;
    private readonly IIdentityGenerator _identityGenerator = Substitute.For<IIdentityGenerator>();
    private readonly IBookmarkQueueSignaler _signaler = Substitute.For<IBookmarkQueueSignaler>();
    private readonly MemoryBookmarkQueueStore _queueStore;
    private readonly MemoryBookmarkQueueDeadLetterStore _deadLetterStore;

    public BookmarkQueueDeadLetterTests()
    {
        _clock = new(_now);
        _identityGenerator.GenerateId().Returns("generated-1", "generated-2", "generated-3");
        _queueStore = new(new MemoryStore<BookmarkQueueItem>());
        _deadLetterStore = new(new MemoryStore<BookmarkQueueDeadLetterItem>());
    }

    [Fact]
    public async Task PurgeAsync_MovesExpiredQueueItemsToDeadLetterBeforeDeleting()
    {
        var expired = NewQueueItem("expired", _now.AddMinutes(-2));
        var current = NewQueueItem("current", _now);
        await _queueStore.AddAsync(expired);
        await _queueStore.AddAsync(current);
        var purger = CreatePurger(new BookmarkQueuePurgeOptions { Ttl = TimeSpan.FromMinutes(1), DeadLetterTtl = TimeSpan.FromDays(7) });

        await purger.PurgeAsync();

        var remainingQueueItems = (await _queueStore.FindManyAsync(new BookmarkQueueFilter())).ToList();
        var deadLetters = (await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter())).ToList();
        Assert.Single(remainingQueueItems);
        Assert.Equal("current", remainingQueueItems[0].Id);
        Assert.Single(deadLetters);
        Assert.Equal("expired", deadLetters[0].OriginalQueueItemId);
        Assert.Equal("Expired", deadLetters[0].Reason);
        Assert.Equal(_now, deadLetters[0].DeadLetteredAt);
        Assert.True(deadLetters[0].CanReplay);
    }

    [Fact]
    public async Task PurgeAsync_DeletesExpiredQueueItemsInBatch()
    {
        var expired1 = NewQueueItem("expired-1", _now.AddMinutes(-3));
        var expired2 = NewQueueItem("expired-2", _now.AddMinutes(-2));
        await _queueStore.AddAsync(expired1);
        await _queueStore.AddAsync(expired2);
        var queueStore = new RecordingDeleteQueueStore(_queueStore);
        var purger = new DefaultBookmarkQueuePurger(
            queueStore,
            _deadLetterStore,
            CreateManager(),
            _clock,
            Microsoft.Extensions.Options.Options.Create(new BookmarkQueuePurgeOptions { Ttl = TimeSpan.FromMinutes(1), DeadLetterTtl = TimeSpan.FromDays(7), BatchSize = 10 }),
            NullLogger<DefaultBookmarkQueuePurger>.Instance);

        await purger.PurgeAsync();

        var deleteFilter = Assert.Single(queueStore.DeleteFilters);
        Assert.Null(deleteFilter.Id);
        Assert.Equal(["expired-1", "expired-2"], deleteFilter.Ids);
        Assert.Empty(await _queueStore.FindManyAsync(new BookmarkQueueFilter()));
        Assert.Equal(2, (await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter())).Count());
    }

    [Fact]
    public async Task PurgeAsync_WhenExpiredItemWasAlreadyRemoved_LeavesDeadLetterForAuditOnly()
    {
        var expired = NewQueueItem("expired", _now.AddMinutes(-2));
        await _queueStore.AddAsync(expired);
        var queueStore = new DeletingAfterPageQueueStore(_queueStore);
        var purger = new DefaultBookmarkQueuePurger(
            queueStore,
            _deadLetterStore,
            CreateManager(),
            _clock,
            Microsoft.Extensions.Options.Options.Create(new BookmarkQueuePurgeOptions { Ttl = TimeSpan.FromMinutes(1), DeadLetterTtl = TimeSpan.FromDays(7) }),
            NullLogger<DefaultBookmarkQueuePurger>.Instance);

        await purger.PurgeAsync();

        var deadLetter = Assert.Single((await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter())).ToList());
        Assert.Equal("expired", deadLetter.OriginalQueueItemId);
        Assert.False(deadLetter.CanReplay);
        Assert.Null(deadLetter.ReplayedAt);
        Assert.Null(deadLetter.ReplayedQueueItemId);

        var replayResult = await CreateManager().ReplayAsync(deadLetter.Id);

        Assert.False(replayResult.Succeeded);
        Assert.Equal(ReplayBookmarkQueueDeadLetterResult.ReasonNotReplayable, replayResult.Reason);
        Assert.Empty(await _queueStore.FindManyAsync(new BookmarkQueueFilter()));
        await _signaler.DidNotReceive().TriggerAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PurgeAsync_WhenOneExpiredItemWasAlreadyRemoved_KeepsReplayForDeletedItemsOnly()
    {
        var removed = NewQueueItem("removed", _now.AddMinutes(-3));
        var retained = NewQueueItem("retained", _now.AddMinutes(-2));
        await _queueStore.AddAsync(removed);
        await _queueStore.AddAsync(retained);
        var queueStore = new DeletingFirstAfterPageQueueStore(_queueStore);
        var purger = new DefaultBookmarkQueuePurger(
            queueStore,
            _deadLetterStore,
            CreateManager(),
            _clock,
            Microsoft.Extensions.Options.Options.Create(new BookmarkQueuePurgeOptions { Ttl = TimeSpan.FromMinutes(1), DeadLetterTtl = TimeSpan.FromDays(7), BatchSize = 10 }),
            NullLogger<DefaultBookmarkQueuePurger>.Instance);

        await purger.PurgeAsync();

        var deadLetters = (await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter())).OrderBy(x => x.OriginalQueueItemId).ToList();
        Assert.Equal(["removed", "retained"], deadLetters.Select(x => x.OriginalQueueItemId));
        Assert.False(deadLetters[0].CanReplay);
        Assert.True(deadLetters[1].CanReplay);
        Assert.Empty(await _queueStore.FindManyAsync(new BookmarkQueueFilter()));
    }

    [Fact]
    public async Task PurgeAsync_WhenExpiredItemAlreadyHasFailedDeadLetter_PreservesReplay()
    {
        var expired = NewQueueItem("expired", _now.AddMinutes(-2));
        await _queueStore.AddAsync(expired);
        await _deadLetterStore.AddAsync(new BookmarkQueueDeadLetterItem
        {
            Id = "failed-dead-letter",
            OriginalQueueItemId = expired.Id,
            WorkflowInstanceId = expired.WorkflowInstanceId,
            BookmarkId = expired.BookmarkId,
            StimulusHash = expired.StimulusHash,
            ActivityTypeName = expired.ActivityTypeName,
            OriginalCreatedAt = expired.CreatedAt,
            DeadLetteredAt = _now.AddMinutes(-1),
            Reason = "Failed",
            DeliveryAttempts = 2,
            CanReplay = true
        });
        var queueStore = new DeletingAfterPageQueueStore(_queueStore);
        var purger = new DefaultBookmarkQueuePurger(
            queueStore,
            _deadLetterStore,
            CreateManager(),
            _clock,
            Microsoft.Extensions.Options.Options.Create(new BookmarkQueuePurgeOptions { Ttl = TimeSpan.FromMinutes(1), DeadLetterTtl = TimeSpan.FromDays(7) }),
            NullLogger<DefaultBookmarkQueuePurger>.Instance);

        await purger.PurgeAsync();

        var deadLetter = Assert.Single((await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter())).ToList());
        Assert.Equal("Failed", deadLetter.Reason);
        Assert.True(deadLetter.CanReplay);
        Assert.Null(deadLetter.ReplayedAt);
        Assert.Null(deadLetter.ReplayedQueueItemId);

        var replayResult = await CreateManager().ReplayAsync(deadLetter.Id);

        Assert.True(replayResult.Succeeded);
        Assert.Equal("generated-1", replayResult.QueueItemId);
        var replayedItem = await _queueStore.FindAsync(new BookmarkQueueFilter { Id = "generated-1" });
        Assert.NotNull(replayedItem);
        await _signaler.Received(1).TriggerAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PurgeAsync_WhenDeadLetterWriteFails_DoesNotDeleteQueueItem()
    {
        var expired = NewQueueItem("expired", _now.AddMinutes(-2));
        await _queueStore.AddAsync(expired);
        var deadLetterStore = new ThrowingAddDeadLetterStore(_deadLetterStore);
        var purger = new DefaultBookmarkQueuePurger(
            _queueStore,
            deadLetterStore,
            CreateManager(deadLetterStore),
            _clock,
            Microsoft.Extensions.Options.Options.Create(new BookmarkQueuePurgeOptions { Ttl = TimeSpan.FromMinutes(1), DeadLetterTtl = TimeSpan.FromDays(7) }),
            NullLogger<DefaultBookmarkQueuePurger>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => purger.PurgeAsync());

        var retained = await _queueStore.FindAsync(new BookmarkQueueFilter { Id = "expired" });
        Assert.NotNull(retained);
        Assert.Empty(await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter()));
    }

    [Fact]
    public async Task PurgeAsync_RemovesDeadLettersAfterRetentionTtl()
    {
        await _deadLetterStore.AddAsync(new BookmarkQueueDeadLetterItem
        {
            Id = "old",
            OriginalQueueItemId = "queue-old",
            DeadLetteredAt = _now.AddDays(-8),
            OriginalCreatedAt = _now.AddDays(-9),
            Reason = "Expired"
        });
        var purger = CreatePurger(new BookmarkQueuePurgeOptions { DeadLetterTtl = TimeSpan.FromDays(7) });

        await purger.PurgeAsync();

        var deadLetters = await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter());
        Assert.Empty(deadLetters);
    }

    [Fact]
    public async Task ProcessAsync_DeadLettersQueueItemAfterMaxDeliveryAttempts()
    {
        var item = NewQueueItem("failed", _now);
        await _queueStore.AddAsync(item);
        var workflowResumer = new ThrowingWorkflowResumer();
        var processor = new BookmarkQueueProcessor(
            _queueStore,
            CreateManager(),
            workflowResumer,
            _clock,
            Microsoft.Extensions.Options.Options.Create(new BookmarkQueuePurgeOptions { MaxDeliveryAttempts = 2 }),
            NullLogger<BookmarkQueueProcessor>.Instance);

        await processor.ProcessAsync();
        var retained = await _queueStore.FindAsync(new BookmarkQueueFilter { Id = "failed" });
        Assert.NotNull(retained);
        Assert.Equal(1, retained.DeliveryAttempts);

        await processor.ProcessAsync();

        Assert.Null(await _queueStore.FindAsync(new BookmarkQueueFilter { Id = "failed" }));
        var deadLetter = Assert.Single((await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter())).ToList());
        Assert.Equal("failed", deadLetter.OriginalQueueItemId);
        Assert.Equal("Failed", deadLetter.Reason);
        Assert.Equal(2, deadLetter.DeliveryAttempts);
        Assert.Equal(typeof(ApplicationException).FullName, deadLetter.LastErrorType);
        Assert.Equal("resume failed", deadLetter.LastErrorMessage);
    }

    [Fact]
    public async Task ProcessAsync_WhenResumeIsCanceled_PropagatesCancellationWithoutIncrementingDeliveryAttempts()
    {
        var item = NewQueueItem("canceled", _now);
        await _queueStore.AddAsync(item);
        var processor = new BookmarkQueueProcessor(
            _queueStore,
            CreateManager(),
            new CancelingWorkflowResumer(),
            _clock,
            Microsoft.Extensions.Options.Options.Create(new BookmarkQueuePurgeOptions { MaxDeliveryAttempts = 2 }),
            NullLogger<BookmarkQueueProcessor>.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(() => processor.ProcessAsync());

        var retained = await _queueStore.FindAsync(new BookmarkQueueFilter { Id = "canceled" });
        Assert.NotNull(retained);
        Assert.Equal(0, retained.DeliveryAttempts);
        Assert.Empty(await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter()));
    }

    [Fact]
    public async Task ProcessAsync_WhenResumeThrowsBuiltInException_DeadLettersQueueItem()
    {
        var item = NewQueueItem("built-in-exception", _now);
        await _queueStore.AddAsync(item);
        var processor = new BookmarkQueueProcessor(
            _queueStore,
            CreateManager(),
            new ThrowingWorkflowResumer(new InvalidOperationException("transient failure")),
            _clock,
            Microsoft.Extensions.Options.Options.Create(new BookmarkQueuePurgeOptions { MaxDeliveryAttempts = 1 }),
            NullLogger<BookmarkQueueProcessor>.Instance);

        await processor.ProcessAsync();

        Assert.Null(await _queueStore.FindAsync(new BookmarkQueueFilter { Id = "built-in-exception" }));
        var deadLetter = Assert.Single((await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter())).ToList());
        Assert.Equal("built-in-exception", deadLetter.OriginalQueueItemId);
        Assert.Equal("Failed", deadLetter.Reason);
        Assert.Equal(1, deadLetter.DeliveryAttempts);
        Assert.Equal(typeof(InvalidOperationException).FullName, deadLetter.LastErrorType);
        Assert.Equal("transient failure", deadLetter.LastErrorMessage);
    }

    [Fact]
    public async Task ReplayAsync_EnqueuesItemAndPreventsSecondReplay()
    {
        await _deadLetterStore.AddAsync(new BookmarkQueueDeadLetterItem
        {
            Id = "dead-letter",
            OriginalQueueItemId = "original",
            WorkflowInstanceId = "workflow-instance",
            BookmarkId = "bookmark",
            StimulusHash = "hash",
            ActivityTypeName = "activity",
            OriginalCreatedAt = _now.AddMinutes(-2),
            DeadLetteredAt = _now.AddMinutes(-1),
            Reason = "Expired",
            CanReplay = true
        });
        var manager = CreateManager();

        var result = await manager.ReplayAsync("dead-letter");

        Assert.True(result.Succeeded);
        Assert.Equal("generated-1", result.QueueItemId);
        var queueItem = await _queueStore.FindAsync(new BookmarkQueueFilter { Id = "generated-1" });
        Assert.NotNull(queueItem);
        Assert.Equal("workflow-instance", queueItem.WorkflowInstanceId);
        await _signaler.Received(1).TriggerAsync(Arg.Any<CancellationToken>());

        var secondResult = await manager.ReplayAsync("dead-letter");

        Assert.False(secondResult.Succeeded);
        Assert.Equal("NotReplayable", secondResult.Reason);
        await _signaler.Received(1).TriggerAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenCalledConcurrently_EnqueuesSingleItem()
    {
        await _deadLetterStore.AddAsync(new BookmarkQueueDeadLetterItem
        {
            Id = "dead-letter",
            OriginalQueueItemId = "original",
            WorkflowInstanceId = "workflow-instance",
            BookmarkId = "bookmark",
            StimulusHash = "hash",
            ActivityTypeName = "activity",
            OriginalCreatedAt = _now.AddMinutes(-2),
            DeadLetteredAt = _now.AddMinutes(-1),
            Reason = "Expired",
            CanReplay = true
        });
        var manager = CreateManager();

        var results = await Task.WhenAll(
            manager.ReplayAsync("dead-letter"),
            manager.ReplayAsync("dead-letter"));

        Assert.Single(results, x => x.Succeeded);
        Assert.Single(results, x => !x.Succeeded && x.Reason == ReplayBookmarkQueueDeadLetterResult.ReasonNotReplayable);
        var queueItems = await _queueStore.FindManyAsync(new BookmarkQueueFilter());
        Assert.Single(queueItems);
        await _signaler.Received(1).TriggerAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_WhenQueueEnqueueFails_RestoresDeadLetterReplayState()
    {
        await _deadLetterStore.AddAsync(new BookmarkQueueDeadLetterItem
        {
            Id = "dead-letter",
            OriginalQueueItemId = "original",
            WorkflowInstanceId = "workflow-instance",
            BookmarkId = "bookmark",
            StimulusHash = "hash",
            ActivityTypeName = "activity",
            OriginalCreatedAt = _now.AddMinutes(-2),
            DeadLetteredAt = _now.AddMinutes(-1),
            Reason = "Expired",
            CanReplay = true
        });
        var inspectingDeadLetterStore = new InspectingSaveDeadLetterStore(_deadLetterStore, async () =>
        {
            var marked = await _deadLetterStore.FindAsync(new BookmarkQueueDeadLetterFilter { Id = "dead-letter" });
            Assert.NotNull(marked);
            Assert.False(marked.CanReplay);
            Assert.Equal(_now, marked.ReplayedAt);
            Assert.Equal("generated-1", marked.ReplayedQueueItemId);
        });
        var manager = new BookmarkQueueDeadLetterManager(
            inspectingDeadLetterStore,
            new ThrowingAddQueueStore(_queueStore),
            _signaler,
            _clock,
            _identityGenerator,
            NullLogger<BookmarkQueueDeadLetterManager>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ReplayAsync("dead-letter"));

        var deadLetter = await _deadLetterStore.FindAsync(new BookmarkQueueDeadLetterFilter { Id = "dead-letter" });
        Assert.NotNull(deadLetter);
        Assert.True(deadLetter.CanReplay);
        Assert.Null(deadLetter.ReplayedAt);
        Assert.Null(deadLetter.ReplayedQueueItemId);
        Assert.Empty(await _queueStore.FindManyAsync(new BookmarkQueueFilter()));
        await _signaler.DidNotReceive().TriggerAsync(Arg.Any<CancellationToken>());

        var retryResult = await CreateManager().ReplayAsync("dead-letter");

        Assert.True(retryResult.Succeeded);
        Assert.Equal("generated-2", retryResult.QueueItemId);
    }

    [Fact]
    public async Task DeadLetterAsync_WhenOriginalQueueItemAlreadyDeadLettered_ReturnsExistingItem()
    {
        var item = NewQueueItem("original", _now.AddMinutes(-2));
        var manager = CreateManager();

        var first = await manager.DeadLetterAsync(item, "Expired");
        var second = await manager.DeadLetterAsync(item, "Failed", new InvalidOperationException("resume failed"));

        Assert.Equal(first.Id, second.Id);
        var deadLetters = await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter());
        Assert.Single(deadLetters);
        Assert.Equal("Expired", first.Reason);
    }

    [Fact]
    public async Task DeadLetterAsync_WhenCalledConcurrentlyForSameQueueItem_StoresSingleDeadLetterItem()
    {
        var item = NewQueueItem("original", _now.AddMinutes(-2));
        var deadLetterStore = new CoordinatedFindDeadLetterStore(_deadLetterStore, 2);
        var manager = CreateManager(deadLetterStore);

        var results = await Task.WhenAll(
            manager.DeadLetterAsync(item, "Expired"),
            manager.DeadLetterAsync(item, "Failed", new InvalidOperationException("resume failed")));

        Assert.Equal(results[0].Id, results[1].Id);
        var deadLetters = (await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter())).ToList();
        var deadLetter = Assert.Single(deadLetters);
        Assert.Equal("original", deadLetter.OriginalQueueItemId);
        Assert.Equal(deadLetter.Id, results[0].Id);
    }

    [Fact]
    public async Task MemoryStore_AddAsync_WhenCalledConcurrentlyForSameOriginalQueueItem_StoresSingleDeadLetterItem()
    {
        var records = Enumerable.Range(1, 20)
            .Select(i => new BookmarkQueueDeadLetterItem
            {
                Id = $"dead-letter-{i}",
                OriginalQueueItemId = "original",
                OriginalCreatedAt = _now.AddMinutes(-2),
                DeadLetteredAt = _now,
                Reason = "Expired",
                CanReplay = true
            })
            .ToList();

        await Task.WhenAll(records.Select(record => Task.Run(() => _deadLetterStore.AddAsync(record))));

        var deadLetter = Assert.Single((await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter())).ToList());
        Assert.Equal("original", deadLetter.OriginalQueueItemId);
        Assert.Contains(deadLetter.Id, records.Select(x => x.Id));
    }

    private DefaultBookmarkQueuePurger CreatePurger(BookmarkQueuePurgeOptions options)
    {
        return new(
            _queueStore,
            _deadLetterStore,
            CreateManager(),
            _clock,
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<DefaultBookmarkQueuePurger>.Instance);
    }

    private BookmarkQueueDeadLetterManager CreateManager(IBookmarkQueueDeadLetterStore? deadLetterStore = null)
    {
        return new(
            deadLetterStore ?? _deadLetterStore,
            _queueStore,
            _signaler,
            _clock,
            _identityGenerator,
            NullLogger<BookmarkQueueDeadLetterManager>.Instance);
    }

    private static BookmarkQueueItem NewQueueItem(string id, DateTimeOffset createdAt)
    {
        return new()
        {
            Id = id,
            WorkflowInstanceId = "workflow-instance",
            BookmarkId = "bookmark",
            StimulusHash = "hash",
            ActivityTypeName = "activity",
            CreatedAt = createdAt
        };
    }

    private sealed class TestClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class ThrowingWorkflowResumer(Exception? exception = null) : IWorkflowResumer
    {
        private readonly Exception _exception = exception ?? new ApplicationException("resume failed");

        public Task<IEnumerable<RunWorkflowInstanceResponse>> ResumeAsync<TActivity>(object stimulus, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default) where TActivity : IActivity
        {
            throw new NotSupportedException();
        }

        public Task<RunWorkflowInstanceResponse?> ResumeAsync(string bookmarkId, IDictionary<string, object> input, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IEnumerable<RunWorkflowInstanceResponse>> ResumeAsync<TActivity>(object stimulus, string? workflowInstanceId = null, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default) where TActivity : IActivity
        {
            throw new NotSupportedException();
        }

        public Task<RunWorkflowInstanceResponse?> ResumeAsync<TActivity>(string bookmarkId, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default) where TActivity : IActivity
        {
            throw new NotSupportedException();
        }

        public Task<IEnumerable<RunWorkflowInstanceResponse>> ResumeAsync(ResumeBookmarkRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IEnumerable<RunWorkflowInstanceResponse>> ResumeAsync(BookmarkFilter filter, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw _exception;
        }
    }

    private sealed class CancelingWorkflowResumer : IWorkflowResumer
    {
        public Task<IEnumerable<RunWorkflowInstanceResponse>> ResumeAsync<TActivity>(object stimulus, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default) where TActivity : IActivity
        {
            throw new NotSupportedException();
        }

        public Task<RunWorkflowInstanceResponse?> ResumeAsync(string bookmarkId, IDictionary<string, object> input, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IEnumerable<RunWorkflowInstanceResponse>> ResumeAsync<TActivity>(object stimulus, string? workflowInstanceId = null, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default) where TActivity : IActivity
        {
            throw new NotSupportedException();
        }

        public Task<RunWorkflowInstanceResponse?> ResumeAsync<TActivity>(string bookmarkId, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default) where TActivity : IActivity
        {
            throw new NotSupportedException();
        }

        public Task<IEnumerable<RunWorkflowInstanceResponse>> ResumeAsync(ResumeBookmarkRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IEnumerable<RunWorkflowInstanceResponse>> ResumeAsync(BookmarkFilter filter, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class DeletingAfterPageQueueStore(IBookmarkQueueStore inner) : IBookmarkQueueStore
    {
        public Task SaveAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default) => inner.SaveAsync(record, cancellationToken);

        public Task AddAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default) => inner.AddAsync(record, cancellationToken);

        public Task<BookmarkQueueItem?> FindAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);

        public Task<IEnumerable<BookmarkQueueItem>> FindManyAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.FindManyAsync(filter, cancellationToken);

        public Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) => inner.PageAsync(pageArgs, orderBy, cancellationToken);

        public async Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueFilter filter, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
        {
            var page = await inner.PageAsync(pageArgs, filter, orderBy, cancellationToken);
            foreach (var item in page.Items)
                await inner.DeleteAsync(new BookmarkQueueFilter { Id = item.Id }, cancellationToken);

            return page;
        }

        public Task<long> DeleteAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.DeleteAsync(filter, cancellationToken);
    }

    private sealed class DeletingFirstAfterPageQueueStore(IBookmarkQueueStore inner) : IBookmarkQueueStore
    {
        private bool _deleted;

        public Task SaveAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default) => inner.SaveAsync(record, cancellationToken);

        public Task AddAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default) => inner.AddAsync(record, cancellationToken);

        public Task<BookmarkQueueItem?> FindAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);

        public Task<IEnumerable<BookmarkQueueItem>> FindManyAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.FindManyAsync(filter, cancellationToken);

        public Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) => inner.PageAsync(pageArgs, orderBy, cancellationToken);

        public async Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueFilter filter, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
        {
            var page = await inner.PageAsync(pageArgs, filter, orderBy, cancellationToken);
            if (!_deleted && page.Items.Count > 0)
            {
                _deleted = true;
                await inner.DeleteAsync(new BookmarkQueueFilter { Id = page.Items.First().Id }, cancellationToken);
            }

            return page;
        }

        public Task<long> DeleteAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.DeleteAsync(filter, cancellationToken);
    }

    private sealed class RecordingDeleteQueueStore(IBookmarkQueueStore inner) : IBookmarkQueueStore
    {
        public List<BookmarkQueueFilter> DeleteFilters { get; } = [];

        public Task SaveAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default) => inner.SaveAsync(record, cancellationToken);

        public Task AddAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default) => inner.AddAsync(record, cancellationToken);

        public Task<BookmarkQueueItem?> FindAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);

        public Task<IEnumerable<BookmarkQueueItem>> FindManyAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.FindManyAsync(filter, cancellationToken);

        public Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) => inner.PageAsync(pageArgs, orderBy, cancellationToken);

        public Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueFilter filter, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) =>
            inner.PageAsync(pageArgs, filter, orderBy, cancellationToken);

        public Task<long> DeleteAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default)
        {
            DeleteFilters.Add(filter);
            return inner.DeleteAsync(filter, cancellationToken);
        }
    }

    private sealed class CoordinatedFindDeadLetterStore(IBookmarkQueueDeadLetterStore inner, int participantCount) : IBookmarkQueueDeadLetterStore
    {
        private readonly TaskCompletionSource _allFindsCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _findCount;

        public Task SaveAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default) => inner.SaveAsync(record, cancellationToken);

        public Task AddAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default) => inner.AddAsync(record, cancellationToken);

        public Task<BookmarkQueueDeadLetterItem> AddOrGetExistingAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default) => inner.AddOrGetExistingAsync(record, cancellationToken);

        public Task<BookmarkQueueDeadLetterItem?> TryMarkReplayedAsync(string id, string queueItemId, DateTimeOffset replayedAt, CancellationToken cancellationToken = default) =>
            inner.TryMarkReplayedAsync(id, queueItemId, replayedAt, cancellationToken);

        public async Task<BookmarkQueueDeadLetterItem?> FindAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default)
        {
            var result = await inner.FindAsync(filter, cancellationToken);

            if (filter.OriginalQueueItemId == null)
                return result;

            if (Interlocked.Increment(ref _findCount) == participantCount)
                _allFindsCompleted.SetResult();

            await _allFindsCompleted.Task.WaitAsync(cancellationToken);
            return result;
        }

        public Task<IEnumerable<BookmarkQueueDeadLetterItem>> FindManyAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default) =>
            inner.FindManyAsync(filter, cancellationToken);

        public Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) =>
            inner.PageAsync(pageArgs, orderBy, cancellationToken);

        public Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterFilter filter, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) =>
            inner.PageAsync(pageArgs, filter, orderBy, cancellationToken);

        public Task<long> DeleteAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default) => inner.DeleteAsync(filter, cancellationToken);
    }

    private sealed class ThrowingAddDeadLetterStore(IBookmarkQueueDeadLetterStore inner) : IBookmarkQueueDeadLetterStore
    {
        public Task SaveAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default) => inner.SaveAsync(record, cancellationToken);

        public Task AddAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default) => throw new InvalidOperationException("dead-letter write failed");

        public Task<BookmarkQueueDeadLetterItem> AddOrGetExistingAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default) => throw new InvalidOperationException("dead-letter write failed");

        public Task<BookmarkQueueDeadLetterItem?> TryMarkReplayedAsync(string id, string queueItemId, DateTimeOffset replayedAt, CancellationToken cancellationToken = default) =>
            inner.TryMarkReplayedAsync(id, queueItemId, replayedAt, cancellationToken);

        public Task<BookmarkQueueDeadLetterItem?> FindAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);

        public Task<IEnumerable<BookmarkQueueDeadLetterItem>> FindManyAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default) =>
            inner.FindManyAsync(filter, cancellationToken);

        public Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) =>
            inner.PageAsync(pageArgs, orderBy, cancellationToken);

        public Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterFilter filter, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) =>
            inner.PageAsync(pageArgs, filter, orderBy, cancellationToken);

        public Task<long> DeleteAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default) => inner.DeleteAsync(filter, cancellationToken);
    }

    private sealed class InspectingSaveDeadLetterStore(IBookmarkQueueDeadLetterStore inner, Func<Task> beforeSaveAsync) : IBookmarkQueueDeadLetterStore
    {
        public async Task SaveAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default)
        {
            await beforeSaveAsync();
            await inner.SaveAsync(record, cancellationToken);
        }

        public Task AddAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default) => inner.AddAsync(record, cancellationToken);

        public Task<BookmarkQueueDeadLetterItem> AddOrGetExistingAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default) => inner.AddOrGetExistingAsync(record, cancellationToken);

        public Task<BookmarkQueueDeadLetterItem?> TryMarkReplayedAsync(string id, string queueItemId, DateTimeOffset replayedAt, CancellationToken cancellationToken = default) =>
            inner.TryMarkReplayedAsync(id, queueItemId, replayedAt, cancellationToken);

        public Task<BookmarkQueueDeadLetterItem?> FindAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);

        public Task<IEnumerable<BookmarkQueueDeadLetterItem>> FindManyAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default) =>
            inner.FindManyAsync(filter, cancellationToken);

        public Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) =>
            inner.PageAsync(pageArgs, orderBy, cancellationToken);

        public Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterFilter filter, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) =>
            inner.PageAsync(pageArgs, filter, orderBy, cancellationToken);

        public Task<long> DeleteAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default) => inner.DeleteAsync(filter, cancellationToken);
    }

    private sealed class ThrowingAddQueueStore(IBookmarkQueueStore inner) : IBookmarkQueueStore
    {
        public Task SaveAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default) => inner.SaveAsync(record, cancellationToken);

        public Task AddAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("enqueue failed");
        }

        public Task<BookmarkQueueItem?> FindAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);

        public Task<IEnumerable<BookmarkQueueItem>> FindManyAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.FindManyAsync(filter, cancellationToken);

        public Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) => inner.PageAsync(pageArgs, orderBy, cancellationToken);

        public Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueFilter filter, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) => inner.PageAsync(pageArgs, filter, orderBy, cancellationToken);

        public Task<long> DeleteAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.DeleteAsync(filter, cancellationToken);
    }
}
