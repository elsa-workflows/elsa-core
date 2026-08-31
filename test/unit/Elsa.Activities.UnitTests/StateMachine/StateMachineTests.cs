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

    [Fact(DisplayName = "StateMachine executes source exit, accepted transition action, target entry and target triggers")]
    public async Task ExecutesAcceptedTransitionPath()
    {
        var context = await ExecuteAndEnterNewStateAsync();

        await CompleteScheduledActivityAsync(context, _payTrigger);
        Assert.True(context.HasScheduledActivity(_newExit));
        Assert.False(context.HasScheduledActivity(_payAction));

        await CompleteScheduledActivityAsync(context, _newExit);
        Assert.True(context.HasScheduledActivity(_payAction));

        await CompleteScheduledActivityAsync(context, _payAction);
        Assert.Equal("Paid", _stateMachine.CurrentState);
        Assert.Equal("Paid", context.GetProperty<string>(CurrentStateProperty));
        Assert.True(context.HasScheduledActivity(_paidEntry));

        await CompleteScheduledActivityAsync(context, _paidEntry);
        Assert.True(context.HasScheduledActivity(_paidTrigger));
        Assert.Equal(ActivityStatus.Running, context.Status);
    }

    [Fact(DisplayName = "StateMachine exposes source entry, trigger, exit, action and target entry order")]
    public async Task AcceptedTransitionExecutesObservableLifecycleOrder()
    {
        var lifecycle = new List<string>();
        var context = await ExecuteAsync();

        Assert.True(context.HasScheduledActivity(_newEntry));
        Assert.False(context.HasScheduledActivity(_payTrigger));

        await CompleteScheduledActivityAsync(context, _newEntry);
        lifecycle.Add("source entry");
        Assert.True(context.HasScheduledActivity(_payTrigger));

        await CompleteScheduledActivityAsync(context, _payTrigger);
        lifecycle.Add("trigger");
        Assert.True(context.HasScheduledActivity(_newExit));
        Assert.False(context.HasScheduledActivity(_payAction));

        await CompleteScheduledActivityAsync(context, _newExit);
        lifecycle.Add("source exit");
        Assert.True(context.HasScheduledActivity(_payAction));

        await CompleteScheduledActivityAsync(context, _payAction);
        lifecycle.Add("action");
        Assert.Equal("Paid", context.GetProperty<string>(CurrentStateProperty));
        Assert.True(context.HasScheduledActivity(_paidEntry));

        await CompleteScheduledActivityAsync(context, _paidEntry);
        lifecycle.Add("target entry");
        Assert.True(context.HasScheduledActivity(_paidTrigger));

        Assert.Equal(new[] { "source entry", "trigger", "source exit", "action", "target entry" }, lifecycle);
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

    [Fact(DisplayName = "StateMachine evaluates a triggerless transition immediately after state entry")]
    public async Task TriggerlessTransitionIsEvaluatedImmediately()
    {
        var payTransition = _stateMachine.Transitions.Single(x => x.Name == "Pay");
        payTransition.Trigger = null;
        payTransition.Condition = null;
        var context = await ExecuteAsync();

        await CompleteScheduledActivityAsync(context, _newEntry);

        Assert.True(context.HasScheduledActivity(_newExit));
        Assert.False(context.HasScheduledActivity(_cancelTrigger));

        await CompleteScheduledActivityAsync(context, _newExit);
        await CompleteScheduledActivityAsync(context, _payAction);

        Assert.Equal("Paid", context.GetProperty<string>(CurrentStateProperty));
        Assert.True(context.HasScheduledActivity(_paidEntry));
    }

    [Fact(DisplayName = "StateMachine schedules event triggers when triggerless transition conditions are false")]
    public async Task FalseTriggerlessConditionAllowsTriggeredTransitions()
    {
        var payTransition = _stateMachine.Transitions.Single(x => x.Name == "Pay");
        payTransition.Trigger = null;
        payTransition.Condition = new(false);
        var context = await ExecuteAsync();

        await CompleteScheduledActivityAsync(context, _newEntry);

        Assert.Equal("New", _stateMachine.CurrentState);
        Assert.False(context.HasScheduledActivity(_newExit));
        Assert.True(context.HasScheduledActivity(_cancelTrigger));
        Assert.Equal(ActivityStatus.Running, context.Status);
    }

    [Fact(DisplayName = "StateMachine leaves an all-false triggerless state active without rescheduling")]
    public async Task AllFalseTriggerlessTransitionsDoNotCompleteOrSpin()
    {
        _stateMachine.Transitions.Remove(_stateMachine.Transitions.Single(x => x.Name == "Cancel"));
        var payTransition = _stateMachine.Transitions.Single(x => x.Name == "Pay");
        payTransition.Trigger = null;
        payTransition.Condition = new(false);
        var context = await ExecuteAsync();

        await CompleteScheduledActivityAsync(context, _newEntry);

        Assert.Equal("New", _stateMachine.CurrentState);
        Assert.Equal(ActivityStatus.Running, context.Status);
        Assert.False(context.HasScheduledActivity(_newExit));
        Assert.False(context.HasScheduledActivity(_payAction));
        Assert.False(context.HasScheduledActivity(_paidEntry));
        Assert.DoesNotContain(context.WorkflowExecutionContext.CompletionCallbacks, x => x.Owner == context);
    }

    [Fact(DisplayName = "StateMachine completes after entering a terminal state")]
    public async Task TerminalStateCompletesStateMachine()
    {
        var stateMachine = new StateMachineActivity
        {
            InitialState = "Done",
            States = { new StateMachineState { Name = "Done" } }
        };

        var context = await ExecuteAsync(stateMachine);

        Assert.Equal("Done", context.GetProperty<string>(CurrentStateProperty));
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.Empty(context.WorkflowExecutionContext.Scheduler.List());
    }

    [Fact(DisplayName = "StateMachine accepts the first eligible triggerless transition in declaration order")]
    public async Task TriggerlessTransitionsUseDeclarationOrder()
    {
        var stateMachine = new StateMachineActivity
        {
            InitialState = "Source",
            States =
            {
                new StateMachineState { Name = "Source" },
                new StateMachineState { Name = "First" },
                new StateMachineState { Name = "Second" }
            },
            Transitions =
            {
                new Transition { Name = "FirstTransition", From = "Source", To = "First", Condition = new(true) },
                new Transition { Name = "SecondTransition", From = "Source", To = "Second", Condition = new(true) }
            }
        };

        var context = await ExecuteAsync(stateMachine);

        Assert.Equal("First", context.GetProperty<string>(CurrentStateProperty));
        Assert.Equal(ActivityStatus.Completed, context.Status);
    }

    [Fact(DisplayName = "StateMachine yields an empty triggerless self-cycle to the workflow scheduler")]
    public async Task EmptyTriggerlessSelfCycleYieldsToScheduler()
    {
        var stateMachine = new StateMachineActivity
        {
            InitialState = "A",
            States = { new StateMachineState { Name = "A" } },
            Transitions =
            {
                new Transition { From = "A", To = "A" }
            }
        };

        var context = await ExecuteAsync(stateMachine);

        Assert.Equal("A", context.GetProperty<string>(CurrentStateProperty));
        Assert.Equal(ActivityStatus.Running, context.Status);

        var continuation = context.WorkflowExecutionContext.Scheduler.Take();
        await CompleteScheduledActivityAsync(context, continuation.Activity);

        Assert.Equal("A", context.GetProperty<string>(CurrentStateProperty));
        Assert.Equal(ActivityStatus.Running, context.Status);
        Assert.Single(context.WorkflowExecutionContext.Scheduler.List());
    }

    [Fact(DisplayName = "StateMachine yields an empty triggerless two-state cycle to the workflow scheduler")]
    public async Task EmptyTriggerlessTwoStateCycleYieldsToScheduler()
    {
        var stateMachine = new StateMachineActivity
        {
            InitialState = "A",
            States =
            {
                new StateMachineState { Name = "A" },
                new StateMachineState { Name = "B" }
            },
            Transitions =
            {
                new Transition { From = "A", To = "B" },
                new Transition { From = "B", To = "A" }
            }
        };

        var context = await ExecuteAsync(stateMachine);

        Assert.Equal("B", context.GetProperty<string>(CurrentStateProperty));
        Assert.Equal(ActivityStatus.Running, context.Status);

        var continuation = context.WorkflowExecutionContext.Scheduler.Take();
        await CompleteScheduledActivityAsync(context, continuation.Activity);

        Assert.Equal("A", context.GetProperty<string>(CurrentStateProperty));
        Assert.Equal(ActivityStatus.Running, context.Status);
        Assert.Single(context.WorkflowExecutionContext.Scheduler.List());
    }

    [Fact(DisplayName = "StateMachine self-transition executes exit, action and entry in order")]
    public async Task SelfTransitionExecutesExitActionAndEntryInOrder()
    {
        var transition = _stateMachine.Transitions.Single(x => x.Name == "Pay");
        transition.To = "New";
        var context = await ExecuteAndEnterNewStateAsync();

        await CompleteScheduledActivityAsync(context, _payTrigger);
        Assert.True(context.HasScheduledActivity(_newExit));
        Assert.False(context.HasScheduledActivity(_payAction));

        await CompleteScheduledActivityAsync(context, _newExit);
        Assert.True(context.HasScheduledActivity(_payAction));

        await CompleteScheduledActivityAsync(context, _payAction);
        Assert.Equal("New", _stateMachine.CurrentState);
        Assert.True(context.HasScheduledActivity(_newEntry));
    }

    [Fact(DisplayName = "StateMachine false transition condition leaves competing triggers active")]
    public async Task FalseConditionLeavesCompetingTriggersActive()
    {
        var context = await ExecuteAndEnterNewStateAsync();
        _stateMachine.Transitions.Single(x => x.Name == "Pay").Condition = new(false);
        var cancelTriggerContext = await CreateScheduledActivityContextAsync(context, _cancelTrigger);
        var cancelBookmark = cancelTriggerContext.CreateBookmark("cancel");
        var scheduledPayTriggerCount = CountScheduledActivities(context, _payTrigger);

        await CompleteScheduledActivityAsync(context, _payTrigger);

        Assert.Equal("New", _stateMachine.CurrentState);
        Assert.False(context.HasScheduledActivity(_payAction));
        Assert.Equal(scheduledPayTriggerCount + 1, CountScheduledActivities(context, _payTrigger));
        Assert.NotEqual(ActivityStatus.Canceled, cancelTriggerContext.Status);
        Assert.Contains(cancelBookmark, context.WorkflowExecutionContext.Bookmarks);
    }

    [Fact(DisplayName = "StateMachine cancels competing outbound triggers when a transition wins")]
    public async Task AcceptedTransitionCancelsCompetingOutboundTriggers()
    {
        var context = await ExecuteAndEnterNewStateAsync();
        var cancelTriggerContext = await CreateScheduledActivityContextAsync(context, _cancelTrigger);
        var cancelBookmark = cancelTriggerContext.CreateBookmark("cancel");

        await CompleteScheduledActivityAsync(context, _payTrigger);

        Assert.Equal(ActivityStatus.Canceled, cancelTriggerContext.Status);
        Assert.DoesNotContain(cancelBookmark, context.WorkflowExecutionContext.Bookmarks);
    }

    [Fact(DisplayName = "StateMachine cancels every distinct competing outbound trigger")]
    public async Task AcceptedTransitionCancelsDistinctCompetingOutboundTriggers()
    {
        var thirdTrigger = new WriteLine("third trigger") { Id = "third-trigger" };
        _stateMachine.Transitions.Add(new Transition
        {
            Name = "CloseFromNew",
            From = "New",
            To = "Closed",
            Trigger = thirdTrigger
        });
        var context = await ExecuteAndEnterNewStateAsync();
        var cancelTriggerContext = await CreateScheduledActivityContextAsync(context, _cancelTrigger);
        var thirdTriggerContext = await CreateScheduledActivityContextAsync(context, thirdTrigger);
        var cancelBookmark = cancelTriggerContext.CreateBookmark("cancel");
        var thirdBookmark = thirdTriggerContext.CreateBookmark("third");

        await CompleteScheduledActivityAsync(context, _payTrigger);

        Assert.Equal(ActivityStatus.Canceled, cancelTriggerContext.Status);
        Assert.Equal(ActivityStatus.Canceled, thirdTriggerContext.Status);
        Assert.DoesNotContain(cancelBookmark, context.WorkflowExecutionContext.Bookmarks);
        Assert.DoesNotContain(thirdBookmark, context.WorkflowExecutionContext.Bookmarks);
    }

    [Fact(DisplayName = "StateMachine rejects transitions that share a trigger instance")]
    public async Task SharedTriggerInstanceIsRejected()
    {
        _stateMachine.Transitions.Single(x => x.Name == "Cancel").Trigger = _payTrigger;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteAsync());

        Assert.Contains("cannot share a Trigger activity", exception.Message);
    }

    [Fact(DisplayName = "StateMachine rejects transition triggers with duplicate activity IDs")]
    public async Task DuplicateTriggerIdIsRejected()
    {
        _cancelTrigger.Id = _payTrigger.Id;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteAsync());

        Assert.Contains("unique ID", exception.Message);
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
        await CompleteScheduledActivityAsync(context, _newExit);
        context.SetProperty(CurrentStateProperty, "Paid");
        await CompleteScheduledActivityAsync(context, _payAction);

        Assert.False(context.HasScheduledActivity(_paidEntry));
    }

    [Fact(DisplayName = "StateMachine ignores stale state exit completions")]
    public async Task IgnoresStaleStateExitCompletions()
    {
        var context = await ExecuteAndEnterNewStateAsync();

        await CompleteScheduledActivityAsync(context, _payTrigger);
        context.SetProperty(CurrentStateProperty, "Paid");
        await CompleteScheduledActivityAsync(context, _newExit);

        Assert.False(context.HasScheduledActivity(_payAction));
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
