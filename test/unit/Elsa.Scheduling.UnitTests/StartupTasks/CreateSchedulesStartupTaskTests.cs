using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.Models;
using Elsa.Scheduling.Options;
using Elsa.Scheduling.StartupTasks;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Elsa.Scheduling.UnitTests.StartupTasks;

public class CreateSchedulesStartupTaskTests
{
    private readonly StoredTrigger[] _triggers =
    [
        new() { Id = "trigger-1", WorkflowDefinitionId = "definition", WorkflowDefinitionVersionId = "version", ActivityId = "activity" }
    ];

    private readonly StoredBookmark[] _bookmarks = [new() { Id = "bookmark-1", Hash = "hash", WorkflowInstanceId = "instance" }];
    private readonly ITriggerStore _triggerStore = Substitute.For<ITriggerStore>();
    private readonly IBookmarkStore _bookmarkStore = Substitute.For<IBookmarkStore>();
    private readonly ITriggerScheduler _triggerScheduler = Substitute.For<ITriggerScheduler>();
    private readonly IBookmarkScheduler _bookmarkScheduler = Substitute.For<IBookmarkScheduler>();
    private readonly IWorkflowInstanceStore _workflowInstanceStore = Substitute.For<IWorkflowInstanceStore>();
    private readonly IBookmarkManager _bookmarkManager = Substitute.For<IBookmarkManager>();
    private readonly SchedulingOptions _options = new() { StartupSchedulePageSize = 1000 };

    public CreateSchedulesStartupTaskTests()
    {
        _triggerStore.FindManyAsync(Arg.Any<TriggerFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(new Page<StoredTrigger>(_triggers, _triggers.Length));
        _bookmarkStore.FindManyAsync(Arg.Any<BookmarkFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(new Page<StoredBookmark>(_bookmarks, _bookmarks.Length));
        _workflowInstanceStore.FindManyIdsAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(call => RunningIds(call.Arg<WorkflowInstanceFilter>()));
    }

    [Fact]
    public void Task_DependsOnPopulateRegistriesStartupTask()
    {
        var dependency = Assert.Single(typeof(CreateSchedulesStartupTask).GetCustomAttributes(typeof(TaskDependencyAttribute), false).Cast<TaskDependencyAttribute>());

        Assert.Equal(typeof(PopulateRegistriesStartupTask), dependency.DependencyTaskType);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutTenantBackgroundQueue_SchedulesImmediately()
    {
        var task = new CreateSchedulesStartupTask(CreateServiceProvider(), OptionsFactory.Create(_options));

        await task.ExecuteAsync(CancellationToken.None);

        await _triggerScheduler.Received(1).ScheduleAsync(Arg.Is<IEnumerable<StoredTrigger>>(x => x.SequenceEqual(_triggers)), Arg.Any<CancellationToken>());
        await _bookmarkScheduler.Received(1).ScheduleAsync(Arg.Is<IEnumerable<StoredBookmark>>(x => x.SequenceEqual(_bookmarks)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithTenantBackgroundQueue_EnqueuesScheduleCreation()
    {
        TenantBackgroundWorkItem? workItem = null;
        var workQueue = Substitute.For<ITenantBackgroundWorkQueue>();
        workQueue.EnqueueAsync(Arg.Do<TenantBackgroundWorkItem>(x => workItem = x), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);
        var serviceProvider = CreateServiceProvider(services => services.AddSingleton(workQueue));
        var task = new CreateSchedulesStartupTask(serviceProvider, OptionsFactory.Create(_options));

        await task.ExecuteAsync(CancellationToken.None);

        await workQueue.Received(1).EnqueueAsync(Arg.Any<TenantBackgroundWorkItem>(), Arg.Any<CancellationToken>());
        await _triggerScheduler.DidNotReceive().ScheduleAsync(Arg.Any<IEnumerable<StoredTrigger>>(), Arg.Any<CancellationToken>());

        Assert.NotNull(workItem);
        await workItem(serviceProvider, CancellationToken.None);

        await _triggerScheduler.Received(1).ScheduleAsync(Arg.Is<IEnumerable<StoredTrigger>>(x => x.SequenceEqual(_triggers)), Arg.Any<CancellationToken>());
        await _bookmarkScheduler.Received(1).ScheduleAsync(Arg.Is<IEnumerable<StoredBookmark>>(x => x.SequenceEqual(_bookmarks)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SchedulesInConfiguredPages()
    {
        var firstTriggerPage = new[] { _triggers[0] };
        var secondTriggerPage = new[] { new StoredTrigger { Id = "trigger-2", WorkflowDefinitionId = "definition", WorkflowDefinitionVersionId = "version", ActivityId = "activity" } };
        var firstBookmarkPage = new[] { _bookmarks[0] };
        var secondBookmarkPage = new[] { new StoredBookmark { Id = "bookmark-2", Hash = "hash", WorkflowInstanceId = "instance" } };
        _options.StartupSchedulePageSize = 1;
        _triggerStore.FindManyAsync(Arg.Any<TriggerFilter>(), Arg.Is<PageArgs>(x => x.Offset == 0 && x.Limit == 1), Arg.Any<CancellationToken>())
            .Returns(new Page<StoredTrigger>(firstTriggerPage, 2));
        _triggerStore.FindManyAsync(Arg.Any<TriggerFilter>(), Arg.Is<PageArgs>(x => x.Offset == 1 && x.Limit == 1), Arg.Any<CancellationToken>())
            .Returns(new Page<StoredTrigger>(secondTriggerPage, 2));
        _bookmarkStore.FindManyAsync(Arg.Any<BookmarkFilter>(), Arg.Is<PageArgs>(x => x.Offset == 0 && x.Limit == 1), Arg.Any<CancellationToken>())
            .Returns(new Page<StoredBookmark>(firstBookmarkPage, 2));
        _bookmarkStore.FindManyAsync(Arg.Any<BookmarkFilter>(), Arg.Is<PageArgs>(x => x.Offset == 1 && x.Limit == 1), Arg.Any<CancellationToken>())
            .Returns(new Page<StoredBookmark>(secondBookmarkPage, 2));
        var task = new CreateSchedulesStartupTask(CreateServiceProvider(), OptionsFactory.Create(_options));

        await task.ExecuteAsync(CancellationToken.None);

        await _triggerScheduler.Received(1).ScheduleAsync(Arg.Is<IEnumerable<StoredTrigger>>(x => x.SequenceEqual(firstTriggerPage)), Arg.Any<CancellationToken>());
        await _triggerScheduler.Received(1).ScheduleAsync(Arg.Is<IEnumerable<StoredTrigger>>(x => x.SequenceEqual(secondTriggerPage)), Arg.Any<CancellationToken>());
        await _bookmarkScheduler.Received(1).ScheduleAsync(Arg.Is<IEnumerable<StoredBookmark>>(x => x.SequenceEqual(firstBookmarkPage)), Arg.Any<CancellationToken>());
        await _bookmarkScheduler.Received(1).ScheduleAsync(Arg.Is<IEnumerable<StoredBookmark>>(x => x.SequenceEqual(secondBookmarkPage)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SkipsAndPurgesOrphanSchedulingBookmarks()
    {
        var missingInstance = Bookmark("missing-bookmark", "missing-instance");
        var finishedInstance = Bookmark("finished-bookmark", "finished-instance");
        var cancelledInstance = Bookmark("cancelled-bookmark", "cancelled-instance");
        var faultedInstance = Bookmark("faulted-bookmark", "faulted-instance");
        var emptyInstanceId = Bookmark("empty-instance-bookmark", "");
        var suspendedInstance = Bookmark("suspended-bookmark", "suspended-instance");
        var bookmarks = new[]
        {
            missingInstance, finishedInstance, cancelledInstance, faultedInstance, emptyInstanceId, suspendedInstance
        };
        BookmarkFilter? purgedFilter = null;
        _bookmarkStore.FindManyAsync(Arg.Any<BookmarkFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(new Page<StoredBookmark>(bookmarks, bookmarks.Length));
        StubBookmarkReload(bookmarks);
        _workflowInstanceStore.FindManyIdsAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(["suspended-instance"]);
        _bookmarkManager.DeleteManyAsync(Arg.Do<BookmarkFilter>(x => purgedFilter = x), Arg.Any<CancellationToken>())
            .Returns(5);
        var task = new CreateSchedulesStartupTask(CreateServiceProvider(), OptionsFactory.Create(_options));

        await task.ExecuteAsync(CancellationToken.None);

        await _bookmarkScheduler.Received(1).ScheduleAsync(
            Arg.Is<IEnumerable<StoredBookmark>>(x => x.SequenceEqual(new[] { suspendedInstance })),
            Arg.Any<CancellationToken>());
        await _bookmarkManager.Received(1).DeleteManyAsync(Arg.Any<BookmarkFilter>(), Arg.Any<CancellationToken>());
        Assert.NotNull(purgedFilter);
        Assert.Equal(
            new[]
            {
                "cancelled-bookmark", "empty-instance-bookmark", "faulted-bookmark", "finished-bookmark", "missing-bookmark"
            },
            purgedFilter.BookmarkIds?.OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotPurgeOrphanWhoseInstanceBecomesRunningBeforePurge()
    {
        var revived = Bookmark("revived-bookmark", "revived-instance");
        var stillMissing = Bookmark("missing-bookmark", "missing-instance");
        var bookmarks = new[] { revived, stillMissing };
        BookmarkFilter? purgedFilter = null;
        _bookmarkStore.FindManyAsync(Arg.Any<BookmarkFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(new Page<StoredBookmark>(bookmarks, bookmarks.Length));
        StubBookmarkReload(bookmarks);
        _workflowInstanceStore.FindManyIdsAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => Array.Empty<string>(), _ => new[] { "revived-instance" });
        _bookmarkManager.DeleteManyAsync(Arg.Do<BookmarkFilter>(x => purgedFilter = x), Arg.Any<CancellationToken>())
            .Returns(1);
        var task = new CreateSchedulesStartupTask(CreateServiceProvider(), OptionsFactory.Create(_options));

        await task.ExecuteAsync(CancellationToken.None);

        await _bookmarkScheduler.DidNotReceive().ScheduleAsync(Arg.Any<IEnumerable<StoredBookmark>>(), Arg.Any<CancellationToken>());
        await _bookmarkManager.Received(1).DeleteManyAsync(Arg.Any<BookmarkFilter>(), Arg.Any<CancellationToken>());
        Assert.NotNull(purgedFilter);
        Assert.Equal(["missing-bookmark"], purgedFilter.BookmarkIds);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotPurgeWhenEverySchedulingBookmarkIsLive()
    {
        var task = new CreateSchedulesStartupTask(CreateServiceProvider(), OptionsFactory.Create(_options));

        await task.ExecuteAsync(CancellationToken.None);

        await _bookmarkManager.DidNotReceive().DeleteManyAsync(Arg.Any<BookmarkFilter>(), Arg.Any<CancellationToken>());
    }

    private ServiceProvider CreateServiceProvider(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_triggerStore);
        services.AddSingleton(_bookmarkStore);
        services.AddSingleton(_triggerScheduler);
        services.AddSingleton(_bookmarkScheduler);
        services.AddSingleton(_workflowInstanceStore);
        services.AddSingleton(_bookmarkManager);
        configureServices?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private void StubBookmarkReload(IEnumerable<StoredBookmark> bookmarks)
    {
        var bookmarkList = bookmarks.ToList();
        _bookmarkStore.FindManyAsync(Arg.Any<BookmarkFilter>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var ids = call.Arg<BookmarkFilter>().BookmarkIds;
                if (ids == null)
                    return bookmarkList.AsEnumerable();

                var idSet = ids.ToHashSet(StringComparer.Ordinal);
                return bookmarkList.Where(x => idSet.Contains(x.Id));
            });
    }

    private static IEnumerable<string> RunningIds(WorkflowInstanceFilter filter)
    {
        return (filter.Ids ?? []).Where(id => !string.IsNullOrWhiteSpace(id));
    }

    private static StoredBookmark Bookmark(string id, string workflowInstanceId) => new()
    {
        Id = id,
        Hash = "hash",
        Name = SchedulingStimulusNames.Delay,
        WorkflowInstanceId = workflowInstanceId
    };
}
