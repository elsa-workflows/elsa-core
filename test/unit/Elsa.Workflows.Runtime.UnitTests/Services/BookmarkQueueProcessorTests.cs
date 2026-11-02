using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.Runtime.Stores;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class BookmarkQueueProcessorTests
{
    private readonly MemoryBookmarkQueueStore _store = new(new MemoryStore<BookmarkQueueItem>());
    private readonly IWorkflowResumer _resumer = Substitute.For<IWorkflowResumer>();
    private readonly ILogger<BookmarkQueueProcessor> _logger = Substitute.For<ILogger<BookmarkQueueProcessor>>();
    private readonly List<int> _attempted = [];
    private Func<int, CancellationToken, Task<IEnumerable<RunWorkflowInstanceResponse>>> _resume = (_, _) => Resumed();

    /// <summary>
    /// Routes resume attempts through controlled outcomes while recording their order.
    /// </summary>
    public BookmarkQueueProcessorTests()
    {
        _resumer.ResumeAsync(Arg.Any<BookmarkFilter>(), Arg.Any<ResumeBookmarkOptions?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var id = int.Parse(call.Arg<BookmarkFilter>().BookmarkId!);
                _attempted.Add(id);
                return _resume(id, call.Arg<CancellationToken>());
            });
    }

    /// <summary>
    /// Attempts all 120 stimuli once despite the mixture of successful deliveries and retained items.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public async Task ProcessAsync_AttemptsEveryItemOnce_WhenItemsAreDeletedOrRetained(int resumeEvery)
    {
        await SeedAsync(120);
        _resume = (i, _) => resumeEvery > 0 && i % resumeEvery == 0 ? Resumed() : NotResumed();

        await CreateProcessor().ProcessAsync();

        Assert.Equal(Enumerable.Range(0, 120), _attempted);
        Assert.Equal(Enumerable.Range(0, 120).Where(i => resumeEvery == 0 || i % resumeEvery != 0), await RemainingIdsAsync());
    }

    /// <summary>
    /// Logs resume failures, retains failed stimuli and reaches later pages without retrying an item within the same pass.
    /// </summary>
    [Theory]
    [InlineData(0, 1)]
    [InlineData(49, 1)]
    [InlineData(50, 1)]
    [InlineData(119, 1)]
    [InlineData(0, 3)]
    [InlineData(49, 3)]
    [InlineData(50, 3)]
    [InlineData(119, 3)]
    public async Task ProcessAsync_LogsAndRetainsFailedItem_AndContinuesAcrossPages(int failedId, int resumeEvery)
    {
        await SeedAsync(120);
        var exception = new InvalidOperationException("Controlled resume failure");
        _resume = (i, _) => i == failedId
            ? Task.FromException<IEnumerable<RunWorkflowInstanceResponse>>(exception)
            : i % resumeEvery == 0 ? Resumed() : NotResumed();

        await CreateProcessor().ProcessAsync();

        Assert.Equal(Enumerable.Range(0, 120), _attempted);
        var expectedRetained = Enumerable.Range(0, 120).Where(i => i == failedId || i % resumeEvery != 0).ToArray();
        Assert.Equal(expectedRetained, await RemainingIdsAsync());
        var warning = Assert.Single(_logger.ReceivedCalls(), call => call.GetMethodInfo().Name == nameof(ILogger.Log)
            && call.GetArguments()[0] is LogLevel.Warning);
        Assert.Same(exception, warning.GetArguments()[3]);
        Assert.Contains($"item-{failedId:D3}", warning.GetArguments()[2]!.ToString());

        _attempted.Clear();
        await CreateProcessor().ProcessAsync();

        Assert.Equal(expectedRetained, _attempted);
        Assert.Equal(expectedRetained, await RemainingIdsAsync());
    }

    /// <summary>
    /// Propagates cancellation from a resume attempt, including when the caller token has not been cancelled.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProcessAsync_PropagatesCancellation_WithoutAttemptingLaterItems(bool cancelCaller)
    {
        await SeedAsync(2);
        using var cancellation = new CancellationTokenSource();
        var exception = new OperationCanceledException(cancellation.Token);
        _resume = (_, token) =>
        {
            Assert.Equal(cancellation.Token, token);
            if (cancelCaller)
            {
                cancellation.Cancel();
            }

            return Task.FromException<IEnumerable<RunWorkflowInstanceResponse>>(exception);
        };

        var observed = await Record.ExceptionAsync(() => CreateProcessor().ProcessAsync(cancellation.Token));

        Assert.Same(exception, observed);
        Assert.Equal([0], _attempted);
        Assert.Equal(new[] { 0, 1 }, await RemainingIdsAsync());
    }

    /// <summary>
    /// Propagates fatal failures without attempting later items.
    /// </summary>
    [Theory]
    [InlineData(typeof(OutOfMemoryException))]
    [InlineData(typeof(StackOverflowException))]
    public async Task ProcessAsync_PropagatesFatalExceptions(Type exceptionType)
    {
        await SeedAsync(2);
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;
        _resume = (_, _) => Task.FromException<IEnumerable<RunWorkflowInstanceResponse>>(exception);

        var observed = await Record.ExceptionAsync(() => CreateProcessor().ProcessAsync());

        Assert.Same(exception, observed);
        Assert.Equal([0], _attempted);
        Assert.Equal(new[] { 0, 1 }, await RemainingIdsAsync());
    }

    /// <summary>
    /// Preserves propagation of persistence failures after a successful resume.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_PropagatesDeleteFailure()
    {
        await SeedAsync(2);
        var store = Substitute.For<IBookmarkQueueStore>();
        store.PageAsync(Arg.Any<PageArgs>(), Arg.Any<BookmarkQueueItemOrder<DateTimeOffset>>(), Arg.Any<CancellationToken>())
            .Returns(call => _store.PageAsync(call.Arg<PageArgs>(), call.Arg<BookmarkQueueItemOrder<DateTimeOffset>>(), call.Arg<CancellationToken>()));
        var exception = new InvalidOperationException("Controlled persistence failure");
        store.DeleteAsync(Arg.Any<BookmarkQueueFilter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<long>(exception));

        var observed = await Record.ExceptionAsync(() => CreateProcessor(store).ProcessAsync());

        Assert.Same(exception, observed);
        Assert.Equal([0], _attempted);
        Assert.Equal(new[] { 0, 1 }, await RemainingIdsAsync());
    }

    /// <summary>
    /// Completes an empty pass without attempting a resume.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_EmptyQueue_DoesNotResumeAnything()
    {
        await CreateProcessor().ProcessAsync();

        Assert.Empty(_attempted);
    }

    /// <summary>
    /// Creates a processor with the in-memory store or a store supplied by a persistence-failure scenario.
    /// </summary>
    private BookmarkQueueProcessor CreateProcessor(IBookmarkQueueStore? store = null) => new(store ?? _store, _resumer, _logger);

    /// <summary>
    /// Creates stimuli with distinct timestamps to give paging a deterministic order.
    /// </summary>
    private async Task SeedAsync(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await _store.AddAsync(new BookmarkQueueItem
            {
                Id = $"item-{i:D3}",
                BookmarkId = i.ToString(),
                CreatedAt = DateTimeOffset.UnixEpoch.AddSeconds(i)
            });
        }
    }

    private async Task<int[]> RemainingIdsAsync() => (await _store.FindManyAsync(new BookmarkQueueFilter()))
        .Select(x => int.Parse(x.BookmarkId!)).Order().ToArray();

    private static Task<IEnumerable<RunWorkflowInstanceResponse>> Resumed() => Task.FromResult<IEnumerable<RunWorkflowInstanceResponse>>([new()]);
    private static Task<IEnumerable<RunWorkflowInstanceResponse>> NotResumed() => Task.FromResult<IEnumerable<RunWorkflowInstanceResponse>>([]);
}
