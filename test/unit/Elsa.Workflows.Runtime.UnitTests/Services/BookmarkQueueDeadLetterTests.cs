using Elsa.Common;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
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
        Assert.True(deadLetters[0].CanReplay);
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
        Assert.Equal(typeof(InvalidOperationException).FullName, deadLetter.LastErrorType);
        Assert.Equal("resume failed", deadLetter.LastErrorMessage);
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
    public async Task DeadLetterAsync_WhenOriginalQueueItemAlreadyDeadLettered_ReturnsExistingItem()
    {
        var item = NewQueueItem("original", _now.AddMinutes(-2));
        var manager = CreateManager();

        var first = await manager.DeadLetterAsync(item, "Expired");
        var second = await manager.DeadLetterAsync(item, "Failed", new InvalidOperationException("resume failed"));

        Assert.Same(first, second);
        var deadLetters = await _deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter());
        Assert.Single(deadLetters);
        Assert.Equal("Expired", first.Reason);
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

    private BookmarkQueueDeadLetterManager CreateManager()
    {
        return new(
            _deadLetterStore,
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

    private sealed class ThrowingWorkflowResumer : IWorkflowResumer
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
            throw new InvalidOperationException("resume failed");
        }
    }
}
