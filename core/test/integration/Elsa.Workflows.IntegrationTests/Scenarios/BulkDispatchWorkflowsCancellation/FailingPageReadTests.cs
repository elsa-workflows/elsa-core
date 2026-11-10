using System.Collections.Concurrent;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BulkDispatchWorkflowsCancellation;

/// <summary>
/// What happens when the cancel cascade cannot read a page of children on the first try, or at all.
/// </summary>
public class FailingPageReadTests(ITestOutputHelper testOutputHelper) : ChildWorkflowCancellationTests(testOutputHelper, builder => Configure(builder))
{
    internal static TestApplicationBuilder Configure(
        TestApplicationBuilder builder,
        int failureCount = 1,
        RejectedCascadeReadCounter? rejectedReads = null,
        bool throwCanceled = false) => builder
        .AddWorkflow<BlockingChildWorkflow>()
        .AddWorkflow<BulkDispatchAndWaitInForkWorkflow>()
        .ConfigureElsa(elsa => elsa.UseWorkflowManagement(management => management.UseWorkflowInstances(instances =>
        {
            var inner = instances.WorkflowInstanceStore;
            instances.WorkflowInstanceStore = sp => new FailingOncePagedReadWorkflowInstanceStore(inner(sp), failureCount, rejectedReads, throwCanceled);
        })));

    [Fact(DisplayName = "A one-time page-read failure is retried and no page is skipped")]
    public async Task CancellingTheWaitingBulkDispatchActivity_WhenTheFirstPageReadFailsOnce_RetriesAndCancelsEveryPage()
    {
        const int childCount = BulkDispatchAndWaitInForkWorkflow.ChildCount;

        // Arrange: more than one page of children, ordered the same way the cancel cascade reads them.
        var parentId = await StartParentAsync(nameof(BulkDispatchAndWaitInForkWorkflow));
        var childIds = (await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), childCount, IsSuspended)).Select(x => x.Id).Order().ToList();

        // Act: the first page read throws once, then succeeds on retry. No page is skipped.
        var resumed = await ResumeAsync(parentId, BulkDispatchAndWaitInForkWorkflow.ProceedActivityId);
        Assert.Equal(WorkflowStatus.Finished, resumed.Status);
        Assert.Equal(WorkflowSubStatus.Finished, resumed.SubStatus);
        Assert.Empty((await ExportStateAsync(parentId)).Incidents);

        Assert.Equal(childIds, CancellationTargets.Order());
    }
}

/// <summary>
/// What happens when every cancel-cascade page read fails, including the retries, until the consecutive-unreadable-page cap.
/// </summary>
public class GivingUpPageReadTests : ChildWorkflowCancellationTests
{
    private readonly RejectedCascadeReadCounter _rejectedReads;
    private readonly WarningLogCapture _logs;

    public GivingUpPageReadTests(ITestOutputHelper testOutputHelper) : this(testOutputHelper, new RejectedCascadeReadCounter(), new WarningLogCapture())
    {
    }

    private GivingUpPageReadTests(ITestOutputHelper testOutputHelper, RejectedCascadeReadCounter rejectedReads, WarningLogCapture logs) : base(
        testOutputHelper,
        builder => FailingPageReadTests.Configure(builder, failureCount: int.MaxValue, rejectedReads)
            .ConfigureServices(services => services.AddLogging(logging => logging.AddProvider(logs))))
    {
        _rejectedReads = rejectedReads;
        _logs = logs;
    }

    [Fact(DisplayName = "Every page is tried three times, and paging gives up after three unreadable pages in a row")]
    public async Task CancellingTheWaitingBulkDispatchActivity_WhenEveryPageReadFails_GivesUpAfterTheCap()
    {
        const int childCount = BulkDispatchAndWaitInForkWorkflow.ChildCount;

        // Arrange: children exist so the waiting activity has something to look for. Every cascade read will throw.
        var parentId = await StartParentAsync(nameof(BulkDispatchAndWaitInForkWorkflow));
        await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), childCount, IsSuspended);

        // Act: each page fails on every attempt and is left uncancelled, then paging moves on. The parent must still finish.
        var resumed = await ResumeAsync(parentId, BulkDispatchAndWaitInForkWorkflow.ProceedActivityId);
        Assert.Equal(WorkflowStatus.Finished, resumed.Status);
        Assert.Equal(WorkflowSubStatus.Finished, resumed.SubStatus);
        Assert.Empty((await ExportStateAsync(parentId)).Incidents);

        Assert.Empty(CancellationTargets);
        // Three pages, three attempts each. The third unreadable page in a row hits the cap and paging stops.
        Assert.Equal(9, _rejectedReads.Count);

        // Each abandoned page is logged with its number, the parent, and the last read failure.
        var pageWarnings = _logs.Entries.Where(x => x.Level == LogLevel.Warning && x.Message.Contains("Left page")).ToList();
        Assert.Equal(3, pageWarnings.Count);
        for (var page = 0; page < 3; page++)
            Assert.Contains(pageWarnings, x => x.Message.Contains($"page {page} of") && x.Message.Contains(parentId) && x.Exception is not null);

        // The give-up warning is logged once, when the cap is reached.
        var giveUp = Assert.Single(_logs.Entries, x => x.Level == LogLevel.Warning && x.Message.Contains("Giving up"));
        Assert.Contains(parentId, giveUp.Message);
        Assert.NotNull(giveUp.Exception);
    }
}

/// <summary>
/// Captures warning-or-higher log entries so a test can assert on what was logged.
/// </summary>
internal sealed class WarningLogCapture : ILoggerProvider
{
    private readonly ConcurrentQueue<LoggedEntry> _entries = new();

    public IEnumerable<LoggedEntry> Entries => _entries;

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(_entries);

    public void Dispose()
    {
    }

    internal sealed record LoggedEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class CapturingLogger(ConcurrentQueue<LoggedEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
                entries.Enqueue(new LoggedEntry(logLevel, formatter(state, exception), exception));
        }
    }
}

/// <summary>
/// What happens when a page read throws an <see cref="OperationCanceledException"/> that is not from the caller's token.
/// </summary>
public class UnrelatedCanceledPageReadTests(ITestOutputHelper testOutputHelper) : ChildWorkflowCancellationTests(testOutputHelper, builder =>
    FailingPageReadTests.Configure(builder, throwCanceled: true))
{
    [Fact(DisplayName = "An unrelated OperationCanceledException on a page read is retried and no page is skipped")]
    public async Task CancellingTheWaitingBulkDispatchActivity_WhenAPageReadThrowsUnrelatedOCE_RetriesAndCancelsEveryPage()
    {
        const int childCount = BulkDispatchAndWaitInForkWorkflow.ChildCount;

        // Arrange: more than one page of children. The first cascade read throws OCE while the caller's token stays live.
        var parentId = await StartParentAsync(nameof(BulkDispatchAndWaitInForkWorkflow));
        var childIds = (await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), childCount, IsSuspended)).Select(x => x.Id).Order().ToList();

        // Act: the unrelated OCE is retried like any other failure. The parent must still finish, and every child is cancelled.
        var resumed = await ResumeAsync(parentId, BulkDispatchAndWaitInForkWorkflow.ProceedActivityId);
        Assert.Equal(WorkflowStatus.Finished, resumed.Status);
        Assert.Equal(WorkflowSubStatus.Finished, resumed.SubStatus);
        Assert.Empty((await ExportStateAsync(parentId)).Incidents);

        Assert.Equal(childIds, CancellationTargets.Order());
    }
}
