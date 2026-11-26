using Elsa.Mediator.Contracts;
using Elsa.Scheduling.Handlers;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;
using NSubstitute;

namespace Elsa.Scheduling.UnitTests.Handlers;

public class DeleteSchedulesTests
{
    [Theory(DisplayName = "Deleting a workflow definition looks up only its triggers with a scheduled trigger name")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefinitionDeleting_LooksUpTriggersByScheduledTriggerNames(bool obsoleteConstructor)
    {
        var triggerStore = Substitute.For<ITriggerStore>();
        var triggerScheduler = Substitute.For<ITriggerScheduler>();
        TriggerFilter? filter = null;
        var scheduledTrigger = new StoredTrigger { Id = "timer", Name = SchedulingStimulusNames.Timer };
        triggerStore.FindManyAsync(Arg.Do<TriggerFilter>(x => filter = x), Arg.Any<CancellationToken>()).Returns([scheduledTrigger]);
        var runtimeOptions = Microsoft.Extensions.Options.Options.Create(new RuntimeOptions { ScheduledTriggerNames = { "Custom.Scheduled" } });
        INotificationHandler<WorkflowDefinitionDeleting> handler = obsoleteConstructor
            ? new DeleteSchedules(triggerScheduler, Substitute.For<IBookmarkScheduler>(), triggerStore, Substitute.For<IBookmarkStore>())
            : new DeleteSchedules(triggerScheduler, Substitute.For<IBookmarkScheduler>(), triggerStore, Substitute.For<IBookmarkStore>(), runtimeOptions);
        string[] expectedNames = obsoleteConstructor ? [SchedulingStimulusNames.Cron, SchedulingStimulusNames.StartAt, SchedulingStimulusNames.Timer] : ["Custom.Scheduled"];

        await handler.HandleAsync(new("definition"), CancellationToken.None);

        Assert.NotNull(filter);
        Assert.Equal("definition", filter.WorkflowDefinitionId);
        Assert.Equal(expectedNames, filter.Names!.Order(StringComparer.Ordinal));
        await triggerScheduler.Received(1).UnscheduleAsync(Arg.Is<IEnumerable<StoredTrigger>>(x => x.Single() == scheduledTrigger), Arg.Any<CancellationToken>());
    }
}
