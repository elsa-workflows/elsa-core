using System.Runtime.CompilerServices;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Testing.Shared;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.State;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BackgroundActivityBookmarks;

/// <summary>
/// Regression tests for #8804. A background activity's bookmarks travel to the resuming run inside a persisted bookmark
/// queue item. They must come back as bookmarks, with typed payloads intact, including from queue items saved before the fix.
/// </summary>
public class Tests
{
    private const string EventName = "OrderShipped";

    [Fact(DisplayName = "Background activity bookmarks survive persisted save and resume with typed payloads")]
    public async Task BookmarksSurviveSaveAndResume()
    {
        var state = await RunToBackgroundBookmarkAsync(saveInLegacyShape: false);

        AssertBookmarkRestored(state);
    }

    [Fact(DisplayName = "Background activity bookmarks saved before #8804 in the ICollection<Bookmark> shape still load")]
    public async Task LegacyShapeStillLoads()
    {
        var state = await RunToBackgroundBookmarkAsync(saveInLegacyShape: true);

        AssertBookmarkRestored(state);
    }

    private static void AssertBookmarkRestored(WorkflowState state)
    {
        Assert.True(state.Incidents.Count == 0, $"The background activity faulted: {state.Incidents.FirstOrDefault()?.Exception?.Message}");
        Assert.Equal(WorkflowSubStatus.Suspended, state.SubStatus);
        Assert.True(state.Bookmarks.Any(IsBackgroundBookmark), "The resumed run did not restore the bookmark the background work created.");
        // DistinctBy is deliberate until #8809 is fixed: AddBookmarks appends the restored bookmarks without de-duplicating.
        var bookmark = Assert.Single(state.Bookmarks.Where(IsBackgroundBookmark).DistinctBy(x => x.Id));
        var payload = Assert.IsType<EventBookmarkPayload>(bookmark.Payload);
        Assert.Equal(EventName, payload.EventName);
    }

    // The bookmark created by the background work, as opposed to the one that schedules the background execution itself.
    private static bool IsBackgroundBookmark(Bookmark bookmark) =>
        bookmark.ActivityId == BookmarkingActivity.ActivityId && bookmark.Payload is not Elsa.Workflows.Runtime.Stimuli.BackgroundActivityStimulus;

    private static async Task<WorkflowState> RunToBackgroundBookmarkAsync(bool saveInLegacyShape)
    {
        await using var connection = new SqliteConnection($"Data Source=bookmarks-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        var signal = new TaskCompletionSource<WorkflowState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var legacyStore = new LegacyShapeObserver();
        var stimulusBookmarkSeen = new StrongBox<bool>(); // Shared: the handler is created per notification.

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddNotificationHandler<CommitSignal, WorkflowStateCommitted>(_ => new(signal, stimulusBookmarkSeen));
                services.AddElsa(elsa => elsa
                    .AddActivity<BookmarkingActivity>()
                    .UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(connection.ConnectionString))));

                if (saveInLegacyShape)
                    services.AddScoped<IBookmarkQueueStore>(sp => new LegacyShapeBookmarkQueueStore(sp.GetRequiredService<EFBookmarkQueueStore>(), sp.GetRequiredService<IPayloadSerializer>(), legacyStore));
            })
            .Build();

        await host.StartAsync();
        try
        {
            using var scope = host.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            await serviceProvider.PopulateRegistriesAsync();
            var workflow = await serviceProvider.GetRequiredService<IWorkflowBuilderFactory>().CreateBuilder()
                .BuildWorkflowAsync(new TestWorkflow(builder => builder.Root = new BookmarkingActivity { Id = BookmarkingActivity.ActivityId }));
            await serviceProvider.GetRequiredService<IWorkflowRegistry>().RegisterAsync(workflow);
            var response = await serviceProvider.GetRequiredService<IWorkflowDispatcher>().DispatchAsync(new DispatchWorkflowDefinitionRequest
            {
                DefinitionVersionId = workflow.DefinitionHandle.DefinitionVersionId!,
                InstanceId = Guid.NewGuid().ToString()
            }, null);
            response.ThrowIfFailed();

            // Fail fast: before #8804 the resumed run faults or never restores the bookmark, so don't wait long.
            var completed = await Task.WhenAny(signal.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.True(completed == signal.Task, "The resumed run did not restore the background activity's bookmark within 5 seconds.");
            var state = await signal.Task;
            if (saveInLegacyShape)
                Assert.True(legacyStore.SavedLegacyShape, "Expected the bookmark queue item to be saved in the pre-#8804 shape.");
            return state;
        }
        finally
        {
            using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await host.StopAsync(stopTimeout.Token);
        }
    }

    /// <summary>Runs in the background and creates a bookmark with a typed payload.</summary>
    [Activity("Elsa.Test", Kind = ActivityKind.Task)]
    private class BookmarkingActivity : CodeActivity
    {
        public const string ActivityId = "BookmarkingActivity";

        public BookmarkingActivity() => RunAsynchronously = true;

        protected override void Execute(ActivityExecutionContext context) => context.CreateBookmark(new EventBookmarkPayload(EventName));
    }

    /// <summary>
    /// Completes once the resumed run has finished with the background activity: it restored the bookmark, faulted, or
    /// consumed the stimulus bookmark without restoring anything. The last case lets the old code fail immediately.
    /// </summary>
    private class CommitSignal(TaskCompletionSource<WorkflowState> signal, StrongBox<bool> stimulusBookmarkSeen) : INotificationHandler<WorkflowStateCommitted>
    {
        public Task HandleAsync(WorkflowStateCommitted notification, CancellationToken cancellationToken)
        {
            var state = notification.WorkflowState;
            var hasStimulusBookmark = state.Bookmarks.Any(x => x.Payload is Elsa.Workflows.Runtime.Stimuli.BackgroundActivityStimulus);
            stimulusBookmarkSeen.Value |= hasStimulusBookmark;

            if (state.Incidents.Any() || state.Bookmarks.Any(IsBackgroundBookmark) || (stimulusBookmarkSeen.Value && !hasStimulusBookmark))
                signal.TrySetResult(state);
            return Task.CompletedTask;
        }
    }

    private class LegacyShapeObserver
    {
        public bool SavedLegacyShape { get; set; }
    }

    /// <summary>
    /// Stores background bookmarks the way Elsa did before #8804: as a <c>List&lt;Bookmark&gt;</c> in the resume options,
    /// which the EF Core store then serializes to JSON in that old shape.
    /// </summary>
    private class LegacyShapeBookmarkQueueStore(IBookmarkQueueStore inner, IPayloadSerializer payloadSerializer, LegacyShapeObserver observer) : IBookmarkQueueStore
    {
        public Task SaveAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default) => inner.SaveAsync(ToLegacyShape(record), cancellationToken);
        public Task AddAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default) => inner.AddAsync(ToLegacyShape(record), cancellationToken);
        public Task<BookmarkQueueItem?> FindAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);
        public Task<IEnumerable<BookmarkQueueItem>> FindManyAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.FindManyAsync(filter, cancellationToken);
        public Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) => inner.PageAsync(pageArgs, orderBy, cancellationToken);
        public Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueFilter filter, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default) => inner.PageAsync(pageArgs, filter, orderBy, cancellationToken);
        public Task<long> DeleteAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default) => inner.DeleteAsync(filter, cancellationToken);

        private BookmarkQueueItem ToLegacyShape(BookmarkQueueItem record)
        {
            var properties = record.Options?.Properties;
            if (properties == null)
                return record;

            foreach (var key in properties.Keys.Where(x => x.StartsWith("__BackgroundActivityBookmarks:")).ToList())
            {
                if (properties[key] is not string json)
                    continue;
                // The typed List<Bookmark> that pre-#8804 code put here; the EF Core store serializes it in the old shape.
                var element = System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("_items");
                properties[key] = payloadSerializer.Deserialize<List<Bookmark>>(element);
                observer.SavedLegacyShape = true;
            }

            return record;
        }
    }
}
