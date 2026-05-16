using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities.StateMachine.Models;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StateMachineActivity = Elsa.Workflows.Activities.StateMachine.Activities.StateMachine;

namespace Elsa.Activities.UnitTests.StateMachine;

public class StateMachineTests
{
    private readonly WriteLine _newEntry = new("enter new") { Id = "new-entry" };
    private readonly WriteLine _newExit = new("exit new") { Id = "new-exit" };
    private readonly WriteLine _paidEntry = new("enter paid") { Id = "paid-entry" };
    private readonly WriteLine _payTrigger = new("pay trigger") { Id = "pay-trigger" };
    private readonly WriteLine _cancelTrigger = new("cancel trigger") { Id = "cancel-trigger" };
    private readonly WriteLine _paidTrigger = new("paid trigger") { Id = "paid-trigger" };
    private readonly WriteLine _payAction = new("pay action") { Id = "pay-action" };
    private readonly StateMachineActivity _stateMachine;

    public StateMachineTests()
    {
        _stateMachine = new StateMachineActivity
        {
            InitialState = "New",
            States =
            {
                new StateMachineState { Name = "New", Entry = _newEntry, Exit = _newExit },
                new StateMachineState { Name = "Paid", Entry = _paidEntry },
                new StateMachineState { Name = "Closed" }
            },
            Transitions =
            {
                new Transition
                {
                    Name = "Pay",
                    From = "New",
                    To = "Paid",
                    Trigger = _payTrigger,
                    Condition = new(true),
                    Action = _payAction
                },
                new Transition
                {
                    Name = "Cancel",
                    From = "New",
                    To = "Closed",
                    Trigger = _cancelTrigger,
                    Condition = new(true)
                },
                new Transition
                {
                    Name = "Close",
                    From = "Paid",
                    To = "Closed",
                    Trigger = _paidTrigger
                }
            }
        };
    }

    [Fact(DisplayName = "StateMachine schedules initial state entry before outbound triggers")]
    public async Task SchedulesInitialStateEntryBeforeOutboundTriggers()
    {
        var context = await ExecuteAsync();

        Assert.Equal("New", _stateMachine.CurrentState);
        Assert.True(context.HasScheduledActivity(_newEntry));
        Assert.False(context.HasScheduledActivity(_payTrigger));
    }

    [Fact(DisplayName = "StateMachine schedules outbound triggers after entry completes")]
    public async Task SchedulesOutboundTriggersAfterEntryCompletes()
    {
        var context = await ExecuteAsync();

        await CompleteScheduledActivityAsync(context, _newEntry);

        Assert.True(context.HasScheduledActivity(_payTrigger));
        Assert.True(context.HasScheduledActivity(_cancelTrigger));
        Assert.Equal(ActivityStatus.Running, context.Status);
    }

    [Fact(DisplayName = "StateMachine executes accepted transition action, source exit, target entry and target triggers")]
    public async Task ExecutesAcceptedTransitionPath()
    {
        var context = await ExecuteAndEnterNewStateAsync();

        await CompleteScheduledActivityAsync(context, _payTrigger);
        Assert.True(context.HasScheduledActivity(_payAction));

        await CompleteScheduledActivityAsync(context, _payAction);
        Assert.True(context.HasScheduledActivity(_newExit));

        await CompleteScheduledActivityAsync(context, _newExit);
        Assert.Equal("Paid", _stateMachine.CurrentState);
        Assert.True(context.HasScheduledActivity(_paidEntry));

        await CompleteScheduledActivityAsync(context, _paidEntry);
        Assert.True(context.HasScheduledActivity(_paidTrigger));
        Assert.Equal(ActivityStatus.Running, context.Status);
    }

    [Fact(DisplayName = "StateMachine treats missing transition condition as true")]
    public async Task MissingConditionAllowsTransition()
    {
        var payTransition = _stateMachine.Transitions.Single(x => x.Name == "Pay");
        payTransition.Condition = null;
        payTransition.Action = null;
        _stateMachine.States.Single(x => x.Name == "New").Exit = null;
        _stateMachine.States.Single(x => x.Name == "Paid").Entry = null;
        var context = await ExecuteAndEnterNewStateAsync();

        await CompleteScheduledActivityAsync(context, _payTrigger);

        Assert.Equal("Paid", _stateMachine.CurrentState);
        Assert.True(context.HasScheduledActivity(_paidTrigger));
    }

    [Fact(DisplayName = "StateMachine false transition condition leaves competing triggers active")]
    public async Task FalseConditionLeavesCompetingTriggersActive()
    {
        var context = await ExecuteAndEnterNewStateAsync();
        _stateMachine.Transitions.Single(x => x.Name == "Pay").Condition = new(false);
        var cancelTriggerContext = await CreateScheduledActivityContextAsync(context, _cancelTrigger);
        var scheduledPayTriggerCount = CountScheduledActivities(context, _payTrigger);

        await CompleteScheduledActivityAsync(context, _payTrigger);

        Assert.Equal("New", _stateMachine.CurrentState);
        Assert.False(context.HasScheduledActivity(_payAction));
        Assert.Equal(scheduledPayTriggerCount + 1, CountScheduledActivities(context, _payTrigger));
        Assert.NotEqual(ActivityStatus.Canceled, cancelTriggerContext.Status);
    }

    [Fact(DisplayName = "StateMachine cancels competing outbound triggers when a transition wins")]
    public async Task AcceptedTransitionCancelsCompetingOutboundTriggers()
    {
        var context = await ExecuteAndEnterNewStateAsync();
        var cancelTriggerContext = await CreateScheduledActivityContextAsync(context, _cancelTrigger);

        await CompleteScheduledActivityAsync(context, _payTrigger);

        Assert.Equal(ActivityStatus.Canceled, cancelTriggerContext.Status);
    }

    private async Task<ActivityExecutionContext> ExecuteAndEnterNewStateAsync()
    {
        var context = await ExecuteAsync();
        await CompleteScheduledActivityAsync(context, _newEntry);
        return context;
    }

    private Task<ActivityExecutionContext> ExecuteAsync() => new ActivityTestFixture(_stateMachine)
        .ConfigureServices(services =>
        {
            services.RemoveAll<IWorkflowExecutionContextSchedulerStrategy>();
            services.AddSingleton<IWorkflowExecutionContextSchedulerStrategy, WorkflowExecutionContextSchedulerStrategy>();
        })
        .ExecuteAsync();

    private static async Task CompleteScheduledActivityAsync(ActivityExecutionContext ownerContext, IActivity activity)
    {
        var childContext = await CreateScheduledActivityContextAsync(ownerContext, activity);
        var callback = PopCallback(ownerContext, activity);

        Assert.NotNull(callback?.CompletionCallback);
        await callback!.CompletionCallback!(new ActivityCompletedContext(ownerContext, childContext));
    }

    private static async Task<ActivityExecutionContext> CreateScheduledActivityContextAsync(ActivityExecutionContext ownerContext, IActivity activity)
    {
        var callback = ownerContext.WorkflowExecutionContext.CompletionCallbacks.LastOrDefault(x => x.Owner == ownerContext && x.Child.Activity == activity);
        var childContext = await ownerContext.WorkflowExecutionContext.CreateActivityExecutionContextAsync(activity, new ActivityInvocationOptions
        {
            Owner = ownerContext,
            Tag = callback?.Tag
        });
        childContext.TransitionTo(ActivityStatus.Running);
        ownerContext.WorkflowExecutionContext.AddActivityExecutionContext(childContext);
        return childContext;
    }

    private static int CountScheduledActivities(ActivityExecutionContext context, IActivity activity) =>
        context.WorkflowExecutionContext.Scheduler.List().Count(x => x.Activity == activity);

    private static ActivityCompletionCallbackEntry? PopCallback(ActivityExecutionContext ownerContext, IActivity activity)
    {
        var callback = ownerContext.WorkflowExecutionContext.CompletionCallbacks.LastOrDefault(x => x.Owner == ownerContext && x.Child.Activity == activity);

        if (callback != null)
            ownerContext.WorkflowExecutionContext.RemoveCompletionCallback(callback);

        return callback;
    }
}
