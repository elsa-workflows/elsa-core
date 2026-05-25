using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Scheduling.StartupTasks;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Scheduling.UnitTests.StartupTasks;

public class CreateSchedulesStartupTaskTests
{
    private readonly StoredTrigger[] _triggers = [new() { WorkflowDefinitionId = "definition", WorkflowDefinitionVersionId = "version", ActivityId = "activity" }];
    private readonly StoredBookmark[] _bookmarks = [new() { Hash = "hash", WorkflowInstanceId = "instance" }];
    private readonly ITriggerStore _triggerStore = Substitute.For<ITriggerStore>();
    private readonly IBookmarkStore _bookmarkStore = Substitute.For<IBookmarkStore>();
    private readonly ITriggerScheduler _triggerScheduler = Substitute.For<ITriggerScheduler>();
    private readonly IBookmarkScheduler _bookmarkScheduler = Substitute.For<IBookmarkScheduler>();

    public CreateSchedulesStartupTaskTests()
    {
        _triggerStore.FindManyAsync(Arg.Any<TriggerFilter>(), Arg.Any<CancellationToken>()).Returns(_triggers);
        _bookmarkStore.FindManyAsync(Arg.Any<BookmarkFilter>(), Arg.Any<CancellationToken>()).Returns(_bookmarks);
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
        var task = new CreateSchedulesStartupTask(CreateServiceProvider());

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
        var task = new CreateSchedulesStartupTask(serviceProvider);

        await task.ExecuteAsync(CancellationToken.None);

        await workQueue.Received(1).EnqueueAsync(Arg.Any<TenantBackgroundWorkItem>(), Arg.Any<CancellationToken>());
        await _triggerScheduler.DidNotReceive().ScheduleAsync(Arg.Any<IEnumerable<StoredTrigger>>(), Arg.Any<CancellationToken>());

        Assert.NotNull(workItem);
        await workItem(serviceProvider, CancellationToken.None);

        await _triggerScheduler.Received(1).ScheduleAsync(Arg.Is<IEnumerable<StoredTrigger>>(x => x.SequenceEqual(_triggers)), Arg.Any<CancellationToken>());
        await _bookmarkScheduler.Received(1).ScheduleAsync(Arg.Is<IEnumerable<StoredBookmark>>(x => x.SequenceEqual(_bookmarks)), Arg.Any<CancellationToken>());
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
