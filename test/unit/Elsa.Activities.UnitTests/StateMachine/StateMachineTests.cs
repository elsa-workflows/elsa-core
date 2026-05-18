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
    private const string CurrentStateProperty = "CurrentState";
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
        Assert.Equal("Paid", context.GetProperty<string>(CurrentStateProperty));
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

    [Fact(DisplayName = "StateMachine removes re-armed competing triggers when a different transition wins")]
    public async Task AcceptedTransitionRemovesRearmedCompetingTriggers()
    {
        var context = await ExecuteAndEnterNewStateAsync();
        _stateMachine.Transitions.Single(x => x.Name == "Pay").Condition = new(false);

        await CompleteScheduledActivityAsync(context, _payTrigger);
        Assert.True(context.WorkflowExecutionContext.Scheduler.Any(x => x.Activity == _payTrigger));
        Assert.Contains(context.WorkflowExecutionContext.CompletionCallbacks, x => x.Owner == context && x.Child.Activity == _payTrigger);

        await CompleteScheduledActivityAsync(context, _cancelTrigger);

        Assert.False(context.WorkflowExecutionContext.Scheduler.Any(x => x.Activity == _payTrigger));
        Assert.DoesNotContain(context.WorkflowExecutionContext.CompletionCallbacks, x => x.Owner == context && x.Child.Activity == _payTrigger);
    }

    [Fact(DisplayName = "StateMachine stores current state in the activity execution context")]
    public async Task StoresCurrentStateInActivityExecutionContext()
    {
        var payTransition = _stateMachine.Transitions.Single(x => x.Name == "Pay");
        payTransition.Action = null;
        _stateMachine.States.Single(x => x.Name == "New").Exit = null;
        _stateMachine.States.Single(x => x.Name == "Paid").Entry = null;
        var context = await ExecuteAndEnterNewStateAsync();

        await CompleteScheduledActivityAsync(context, _payTrigger);

        Assert.Equal("Paid", context.GetProperty<string>(CurrentStateProperty));
    }

    [Fact(DisplayName = "StateMachine resolves duplicate unnamed transition endpoints by scheduled transition")]
    public async Task ResolvesDuplicateUnnamedTransitionEndpointsByScheduledTransition()
    {
        var firstTrigger = new WriteLine("first trigger") { Id = "first-trigger" };
        var secondTrigger = new WriteLine("second trigger") { Id = "second-trigger" };
        var firstAction = new WriteLine("first action") { Id = "first-action" };
        var secondAction = new WriteLine("second action") { Id = "second-action" };
        var stateMachine = new StateMachineActivity
        {
            InitialState = "New",
            States =
            {
                new StateMachineState { Name = "New" },
                new StateMachineState { Name = "Paid" }
            },
            Transitions =
            {
                new Transition { From = "New", To = "Paid", Trigger = firstTrigger, Action = firstAction },
                new Transition { From = "New", To = "Paid", Trigger = secondTrigger, Action = secondAction }
            }
        };
        var context = await ExecuteAsync(stateMachine);

        await CompleteScheduledActivityAsync(context, secondTrigger);

        Assert.False(context.HasScheduledActivity(firstAction));
        Assert.True(context.HasScheduledActivity(secondAction));
    }

    [Fact(DisplayName = "StateMachine resolves duplicate named transitions by current state")]
    public async Task ResolvesDuplicateNamedTransitionsByCurrentState()
    {
        var payTrigger = new WriteLine("pay trigger") { Id = "pay-duplicate-trigger" };
        var newCancelTrigger = new WriteLine("new cancel trigger") { Id = "new-cancel-trigger" };
        var paidCancelTrigger = new WriteLine("paid cancel trigger") { Id = "paid-cancel-trigger" };
        var stateMachine = new StateMachineActivity
        {
            InitialState = "New",
            States =
            {
                new StateMachineState { Name = "New" },
                new StateMachineState { Name = "Paid" },
                new StateMachineState { Name = "Closed" }
            },
            Transitions =
            {
                new Transition { Name = "Cancel", From = "New", To = "Closed", Trigger = newCancelTrigger },
                new Transition { Name = "Pay", From = "New", To = "Paid", Trigger = payTrigger },
                new Transition { Name = "Cancel", From = "Paid", To = "Closed", Trigger = paidCancelTrigger }
            }
        };
        var context = await ExecuteAsync(stateMachine);

        await CompleteScheduledActivityAsync(context, payTrigger);
        await CompleteScheduledActivityAsync(context, paidCancelTrigger);

        Assert.Equal("Closed", context.GetProperty<string>(CurrentStateProperty));
    }

    [Fact(DisplayName = "StateMachine ignores stale transition action completions")]
    public async Task IgnoresStaleTransitionActionCompletions()
    {
        var context = await ExecuteAndEnterNewStateAsync();

        await CompleteScheduledActivityAsync(context, _payTrigger);
        context.SetProperty(CurrentStateProperty, "Paid");
        await CompleteScheduledActivityAsync(context, _payAction);

        Assert.False(context.HasScheduledActivity(_newExit));
    }

    private async Task<ActivityExecutionContext> ExecuteAndEnterNewStateAsync()
    {
        var context = await ExecuteAsync();
        await CompleteScheduledActivityAsync(context, _newEntry);
        return context;
    }

    private Task<ActivityExecutionContext> ExecuteAsync(StateMachineActivity? stateMachine = null) => new ActivityTestFixture(stateMachine ?? _stateMachine)
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
