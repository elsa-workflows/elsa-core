using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.Models;
using Elsa.Scheduling.Options;
using Elsa.Scheduling.StartupTasks;
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
    private readonly SchedulingOptions _options = new() { StartupSchedulePageSize = 1000 };

    public CreateSchedulesStartupTaskTests()
    {
        _triggerStore.FindManyAsync(Arg.Any<TriggerFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(new Page<StoredTrigger>(_triggers, _triggers.Length));
        _bookmarkStore.FindManyAsync(Arg.Any<BookmarkFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(new Page<StoredBookmark>(_bookmarks, _bookmarks.Length));
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

    private ServiceProvider CreateServiceProvider(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_triggerStore);
        services.AddSingleton(_bookmarkStore);
        services.AddSingleton(_triggerScheduler);
        services.AddSingleton(_bookmarkScheduler);
        configureServices?.Invoke(services);
        return services.BuildServiceProvider();
    }
}
