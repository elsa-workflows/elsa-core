using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Stores;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class BookmarkQueueProcessorTests
{
    private readonly MemoryBookmarkQueueStore _store;
    private readonly IWorkflowResumer _workflowResumer;
    private readonly BookmarkQueueProcessor _processor;
    private HashSet<string> _failedBookmarkIds = [];

    public BookmarkQueueProcessorTests()
    {
        _store = new MemoryBookmarkQueueStore(new MemoryStore<BookmarkQueueItem>());
        _workflowResumer = Substitute.For<IWorkflowResumer>();
        _workflowResumer
            .ResumeAsync(Arg.Any<BookmarkFilter>(), Arg.Any<ResumeBookmarkOptions?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => CreateResumeResult(callInfo.Arg<BookmarkFilter>()));

        _processor = new BookmarkQueueProcessor(_store, _workflowResumer, NullLogger<BookmarkQueueProcessor>.Instance);
    }

    [Fact]
    public async Task ProcessAsync_WhenAllItemsResumeSuccessfully_DrainsQueueLargerThanBatchSize()
    {
        await SeedQueueAsync(55);

        await _processor.ProcessAsync();

        var remaining = (await _store.FindManyAsync(new BookmarkQueueFilter())).ToList();
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task ProcessAsync_WhenSomeItemsFail_SkipsFailedItemsAndStillProcessesSubsequentPages()
    {
        await SeedQueueAsync(60, "item-05", "item-10");

        await _processor.ProcessAsync();

        var remaining = (await _store.FindManyAsync(new BookmarkQueueFilter())).Select(x => x.Id).OrderBy(x => x).ToList();
        Assert.Equal(["item-05", "item-10"], remaining);
    }

    [Fact]
    public async Task ProcessAsync_WhenRunAgain_RetriesPreviouslyFailedItems()
    {
        await SeedQueueAsync(60, "item-05", "item-10");

        await _processor.ProcessAsync();

        _failedBookmarkIds.Clear();

        await _processor.ProcessAsync();

        var remaining = (await _store.FindManyAsync(new BookmarkQueueFilter())).ToList();
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task ProcessAsync_WhenEntirePageFails_AdvancesToNextPageWithoutReprocessingFailuresInSameRun()
    {
        var failingIds = Enumerable.Range(0, 50).Select(i => $"item-{i:D2}").ToArray();
        await SeedQueueAsync(55, failingIds);
        _failedBookmarkIds = failingIds.ToHashSet();

        await _processor.ProcessAsync();

        var remaining = (await _store.FindManyAsync(new BookmarkQueueFilter())).Select(x => x.Id).OrderBy(x => x).ToList();
        Assert.Equal(Enumerable.Range(0, 50).Select(i => $"item-{i:D2}"), remaining);
    }

    private async Task SeedQueueAsync(int count, params string[] failedIds)
    {
        _failedBookmarkIds = failedIds.ToHashSet();
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var index = 0; index < count; index++)
        {
            var id = $"item-{index:D2}";
            await _store.AddAsync(new BookmarkQueueItem
            {
                Id = id,
                BookmarkId = id,
                WorkflowInstanceId = $"wf-{index}",
                ActivityTypeName = "TestActivity",
                StimulusHash = $"hash-{index}",
                CreatedAt = baseTime.AddSeconds(index),
            });
        }
    }

    private IEnumerable<RunWorkflowInstanceResponse> CreateResumeResult(BookmarkFilter filter)
    {
        if (filter.BookmarkId != null && _failedBookmarkIds.Contains(filter.BookmarkId))
            return [];

        return
        [
            new RunWorkflowInstanceResponse
            {
                WorkflowInstanceId = filter.WorkflowInstanceId ?? "workflow-instance",
            },
        ];
    }
}
