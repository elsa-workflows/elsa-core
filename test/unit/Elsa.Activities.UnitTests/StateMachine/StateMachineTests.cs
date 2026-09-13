using Elsa.Testing.Shared;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Activities.StateMachine.Models;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using StateMachineActivity = Elsa.Workflows.Activities.StateMachine.Activities.StateMachine;
using System.Threading.Tasks;

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

    [Test]
    [DisplayName("StateMachine schedules initial state entry before outbound triggers")]
    public async Task SchedulesInitialStateEntryBeforeOutboundTriggers()
    {
        var context = await ExecuteAsync();

        await Assert.That(_stateMachine.CurrentState).IsEqualTo("New");
        await Assert.That(context.HasScheduledActivity(_newEntry)).IsTrue();
        await Assert.That(context.HasScheduledActivity(_payTrigger)).IsFalse();
    }

    [Test]
    [DisplayName("StateMachine schedules outbound triggers after entry completes")]
    public async Task SchedulesOutboundTriggersAfterEntryCompletes()
    {
        var context = await ExecuteAsync();

        await CompleteScheduledActivityAsync(context, _newEntry);

        await Assert.That(context.HasScheduledActivity(_payTrigger)).IsTrue();
        await Assert.That(context.HasScheduledActivity(_cancelTrigger)).IsTrue();
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Running);
    }

    [Test]
    [DisplayName("StateMachine executes source exit, accepted transition action, target entry and target triggers")]
    public async Task ExecutesAcceptedTransitionPath()
    {
        var context = await ExecuteAndEnterNewStateAsync();

        await CompleteScheduledActivityAsync(context, _payTrigger);
        await Assert.That(context.HasScheduledActivity(_newExit)).IsTrue();
        await Assert.That(context.HasScheduledActivity(_payAction)).IsFalse();

        await CompleteScheduledActivityAsync(context, _newExit);
        await Assert.That(context.HasScheduledActivity(_payAction)).IsTrue();

        await CompleteScheduledActivityAsync(context, _payAction);
        await Assert.That(_stateMachine.CurrentState).IsEqualTo("Paid");
        await Assert.That(context.GetProperty<string>(CurrentStateProperty)).IsEqualTo("Paid");
        await Assert.That(context.HasScheduledActivity(_paidEntry)).IsTrue();

        await CompleteScheduledActivityAsync(context, _paidEntry);
        await Assert.That(context.HasScheduledActivity(_paidTrigger)).IsTrue();
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Running);
    }

    [Test]
    [DisplayName("StateMachine exposes source entry, trigger, exit, action and target entry order")]
    public async Task AcceptedTransitionExecutesObservableLifecycleOrder()
    {
        var lifecycle = new List<string>();
        var context = await ExecuteAsync();

        await Assert.That(context.HasScheduledActivity(_newEntry)).IsTrue();
        await Assert.That(context.HasScheduledActivity(_payTrigger)).IsFalse();

        await CompleteScheduledActivityAsync(context, _newEntry);
        lifecycle.Add("source entry");
        await Assert.That(context.HasScheduledActivity(_payTrigger)).IsTrue();

        await CompleteScheduledActivityAsync(context, _payTrigger);
        lifecycle.Add("trigger");
        await Assert.That(context.HasScheduledActivity(_newExit)).IsTrue();
        await Assert.That(context.HasScheduledActivity(_payAction)).IsFalse();

        await CompleteScheduledActivityAsync(context, _newExit);
        lifecycle.Add("source exit");
        await Assert.That(context.HasScheduledActivity(_payAction)).IsTrue();

        await CompleteScheduledActivityAsync(context, _payAction);
        lifecycle.Add("action");
        await Assert.That(context.GetProperty<string>(CurrentStateProperty)).IsEqualTo("Paid");
        await Assert.That(context.HasScheduledActivity(_paidEntry)).IsTrue();

        await CompleteScheduledActivityAsync(context, _paidEntry);
        lifecycle.Add("target entry");
        await Assert.That(context.HasScheduledActivity(_paidTrigger)).IsTrue();

        await Assert.That(lifecycle).IsEquivalentTo(new[] { "source entry", "trigger", "source exit", "action", "target entry" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("StateMachine treats missing transition condition as true")]
    public async Task MissingConditionAllowsTransition()
    {
        var payTransition = _stateMachine.Transitions.Single(x => x.Name == "Pay");
        payTransition.Condition = null;
        payTransition.Action = null;
        _stateMachine.States.Single(x => x.Name == "New").Exit = null;
        _stateMachine.States.Single(x => x.Name == "Paid").Entry = null;
        var context = await ExecuteAndEnterNewStateAsync();

        await CompleteScheduledActivityAsync(context, _payTrigger);

        await Assert.That(_stateMachine.CurrentState).IsEqualTo("Paid");
        await Assert.That(context.HasScheduledActivity(_paidTrigger)).IsTrue();
    }

    [Test]
    [DisplayName("StateMachine evaluates a triggerless transition immediately after state entry")]
    public async Task TriggerlessTransitionIsEvaluatedImmediately()
    {
        var payTransition = _stateMachine.Transitions.Single(x => x.Name == "Pay");
        payTransition.Trigger = null;
        payTransition.Condition = null;
        var context = await ExecuteAsync();

        await CompleteScheduledActivityAsync(context, _newEntry);

        await Assert.That(context.HasScheduledActivity(_newExit)).IsTrue();
        await Assert.That(context.HasScheduledActivity(_cancelTrigger)).IsFalse();

        await CompleteScheduledActivityAsync(context, _newExit);
        await CompleteScheduledActivityAsync(context, _payAction);

        await Assert.That(context.GetProperty<string>(CurrentStateProperty)).IsEqualTo("Paid");
        await Assert.That(context.HasScheduledActivity(_paidEntry)).IsTrue();
    }

    [Test]
    [DisplayName("StateMachine schedules event triggers when triggerless transition conditions are false")]
    public async Task FalseTriggerlessConditionAllowsTriggeredTransitions()
    {
        var payTransition = _stateMachine.Transitions.Single(x => x.Name == "Pay");
        payTransition.Trigger = null;
        payTransition.Condition = new(false);
        var context = await ExecuteAsync();

        await CompleteScheduledActivityAsync(context, _newEntry);

        await Assert.That(_stateMachine.CurrentState).IsEqualTo("New");
        await Assert.That(context.HasScheduledActivity(_newExit)).IsFalse();
        await Assert.That(context.HasScheduledActivity(_cancelTrigger)).IsTrue();
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Running);
    }

    [Test]
    [DisplayName("StateMachine leaves an all-false triggerless state active without rescheduling")]
    public async Task AllFalseTriggerlessTransitionsDoNotCompleteOrSpin()
    {
        _stateMachine.Transitions.Remove(_stateMachine.Transitions.Single(x => x.Name == "Cancel"));
        var payTransition = _stateMachine.Transitions.Single(x => x.Name == "Pay");
        payTransition.Trigger = null;
        payTransition.Condition = new(false);
        var context = await ExecuteAsync();

        await CompleteScheduledActivityAsync(context, _newEntry);

        await Assert.That(_stateMachine.CurrentState).IsEqualTo("New");
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Running);
        await Assert.That(context.HasScheduledActivity(_newExit)).IsFalse();
        await Assert.That(context.HasScheduledActivity(_payAction)).IsFalse();
        await Assert.That(context.HasScheduledActivity(_paidEntry)).IsFalse();
        await Assert.That(context.WorkflowExecutionContext.CompletionCallbacks).DoesNotContain(x => x.Owner == context);
    }

    [Test]
    [DisplayName("StateMachine completes after entering a terminal state")]
    public async Task TerminalStateCompletesStateMachine()
    {
        var stateMachine = new StateMachineActivity
        {
            InitialState = "Done",
            States = { new StateMachineState { Name = "Done" } }
        };

        var context = await ExecuteAsync(stateMachine);

        await Assert.That(context.GetProperty<string>(CurrentStateProperty)).IsEqualTo("Done");
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Completed);
        await Assert.That(context.WorkflowExecutionContext.Scheduler.List()).IsEmpty();
    }

    [Test]
    [DisplayName("StateMachine accepts the first eligible triggerless transition in declaration order")]
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

        await Assert.That(context.GetProperty<string>(CurrentStateProperty)).IsEqualTo("First");
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Completed);
    }

    [Test]
    [DisplayName("StateMachine yields an empty triggerless self-cycle to the workflow scheduler")]
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

        await Assert.That(context.GetProperty<string>(CurrentStateProperty)).IsEqualTo("A");
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Running);

        var continuation = context.WorkflowExecutionContext.Scheduler.Take();
        await CompleteScheduledActivityAsync(context, continuation.Activity);

        await Assert.That(context.GetProperty<string>(CurrentStateProperty)).IsEqualTo("A");
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Running);
        await Assert.That(context.WorkflowExecutionContext.Scheduler.List()).HasSingleItem();
    }

    [Test]
    [DisplayName("StateMachine yields an empty triggerless two-state cycle to the workflow scheduler")]
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

        await Assert.That(context.GetProperty<string>(CurrentStateProperty)).IsEqualTo("B");
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Running);

        var continuation = context.WorkflowExecutionContext.Scheduler.Take();
        await CompleteScheduledActivityAsync(context, continuation.Activity);

        await Assert.That(context.GetProperty<string>(CurrentStateProperty)).IsEqualTo("A");
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Running);
        await Assert.That(context.WorkflowExecutionContext.Scheduler.List()).HasSingleItem();
    }

    [Test]
    [DisplayName("StateMachine resumes an automatic cycle continuation after state persistence")]
    public async Task AutomaticCycleContinuationSurvivesStateRoundTrip()
    {
        var stateMachine = new StateMachineActivity
        {
            InitialState = "A",
            States = { new StateMachineState { Name = "A" } },
            Transitions = { new Transition { From = "A", To = "A" } }
        };
        var fixture = CreatePersistenceFixture(stateMachine);
        var context = await fixture.BuildAsync();
        context.Id = "state-machine-context";
        context.WorkflowExecutionContext.AddActivityExecutionContext(context);

        await fixture.ExecuteAsync(context);

        var sourceWorkflowContext = context.WorkflowExecutionContext;
        var sourceContinuation = await Assert.That(sourceWorkflowContext.Scheduler.List()).HasSingleItem();
        await Assert.That(sourceContinuation.SchedulingActivityExecutionId).IsEqualTo(context.Id);
        await Assert.That(sourceWorkflowContext.CompletionCallbacks).HasSingleItem();

        var extractor = sourceWorkflowContext.GetRequiredService<IWorkflowStateExtractor>();
        var state = extractor.Extract(sourceWorkflowContext);
        var resumedWorkflowContext = await WorkflowExecutionContext.CreateAsync(
            sourceWorkflowContext.ServiceProvider,
            sourceWorkflowContext.WorkflowGraph,
            state.Id,
            CancellationToken.None);

        await extractor.ApplyAsync(resumedWorkflowContext, state);

        var resumedStateMachineContext = await Assert.That(resumedWorkflowContext.ActivityExecutionContexts).HasSingleItem(x => x.Activity == stateMachine);
        var resumedContinuation = resumedWorkflowContext.Scheduler.Take();
        await Assert.That(state.CompletionCallbacks).HasSingleItem();
        await Assert.That(resumedContinuation.Owner?.Id).IsEqualTo(resumedStateMachineContext.Id);
        await Assert.That(resumedContinuation.SchedulingActivityExecutionId).IsEqualTo(resumedStateMachineContext.Id);
        await Assert.That(resumedWorkflowContext.CompletionCallbacks).Contains(x => x.Owner == resumedStateMachineContext && x.Child.Activity == resumedContinuation.Activity);

        var resumedChildContext = await resumedWorkflowContext.CreateActivityExecutionContextAsync(resumedContinuation.Activity, new ActivityInvocationOptions
        {
            Owner = resumedContinuation.Owner,
            Tag = resumedContinuation.Tag,
            SchedulingActivityExecutionId = resumedContinuation.SchedulingActivityExecutionId
        });
        resumedChildContext.TransitionTo(ActivityStatus.Running);
        resumedWorkflowContext.AddActivityExecutionContext(resumedChildContext);
        await resumedContinuation.Activity.ExecuteAsync(resumedChildContext);

        await Assert.That(resumedStateMachineContext.GetProperty<string>(CurrentStateProperty)).IsEqualTo("A");
        await Assert.That(resumedStateMachineContext.Status).IsEqualTo(ActivityStatus.Running);
        await Assert.That(resumedWorkflowContext.Scheduler.List()).HasSingleItem();
    }

    [Test]
    [DisplayName("StateMachine completes a triggerless transition with a composite action")]
    public async Task TriggerlessTransitionWithCompositeActionCompletes()
    {
        var stateMachine = new StateMachineActivity
        {
            InitialState = "Source",
            States =
            {
                new StateMachineState
                {
                    Name = "Source",
                    Exit = new WriteLine("exit") { Id = "source-exit" }
                },
                new StateMachineState { Name = "Target" }
            },
            Transitions =
            {
                new Transition
                {
                    From = "Source",
                    To = "Target",
                    Condition = new(true),
                    Action = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine("first action") { Id = "first-action" },
                            new WriteLine("second action") { Id = "second-action" }
                        }
                    }
                }
            }
        };
        var fixture = CreatePersistenceFixture(stateMachine);
        var context = await fixture.BuildAsync();
        context.Id = "state-machine-context";
        context.WorkflowExecutionContext.AddActivityExecutionContext(context);

        await fixture.ExecuteAsync(context);

        while (context.WorkflowExecutionContext.Scheduler.HasAny)
            await ExecuteNextScheduledActivityAsync(context.WorkflowExecutionContext);

        await Assert.That(context.GetProperty<string>(CurrentStateProperty)).IsEqualTo("Target");
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Completed);
    }

    [Test]
    [DisplayName("StateMachine self-transition executes exit, action and entry in order")]
    public async Task SelfTransitionExecutesExitActionAndEntryInOrder()
    {
        var transition = _stateMachine.Transitions.Single(x => x.Name == "Pay");
        transition.To = "New";
        var context = await ExecuteAndEnterNewStateAsync();

        await CompleteScheduledActivityAsync(context, _payTrigger);
        await Assert.That(context.HasScheduledActivity(_newExit)).IsTrue();
        await Assert.That(context.HasScheduledActivity(_payAction)).IsFalse();

        await CompleteScheduledActivityAsync(context, _newExit);
        await Assert.That(context.HasScheduledActivity(_payAction)).IsTrue();

        await CompleteScheduledActivityAsync(context, _payAction);
        await Assert.That(_stateMachine.CurrentState).IsEqualTo("New");
        await Assert.That(context.HasScheduledActivity(_newEntry)).IsTrue();
    }

    [Test]
    [DisplayName("StateMachine false transition condition leaves competing triggers active")]
    public async Task FalseConditionLeavesCompetingTriggersActive()
    {
        var context = await ExecuteAndEnterNewStateAsync();
        _stateMachine.Transitions.Single(x => x.Name == "Pay").Condition = new(false);
        var cancelTriggerContext = await CreateScheduledActivityContextAsync(context, _cancelTrigger);
        var cancelBookmark = cancelTriggerContext.CreateBookmark("cancel");
        var scheduledPayTriggerCount = CountScheduledActivities(context, _payTrigger);

        await CompleteScheduledActivityAsync(context, _payTrigger);

        await Assert.That(_stateMachine.CurrentState).IsEqualTo("New");
        await Assert.That(context.HasScheduledActivity(_payAction)).IsFalse();
        await Assert.That(CountScheduledActivities(context, _payTrigger)).IsEqualTo(scheduledPayTriggerCount + 1);
        await Assert.That(cancelTriggerContext.Status).IsNotEqualTo(ActivityStatus.Canceled);
        await Assert.That(context.WorkflowExecutionContext.Bookmarks).Contains(cancelBookmark);
    }

    [Test]
    [DisplayName("StateMachine cancels competing outbound triggers when a transition wins")]
    public async Task AcceptedTransitionCancelsCompetingOutboundTriggers()
    {
        var context = await ExecuteAndEnterNewStateAsync();
        var cancelTriggerContext = await CreateScheduledActivityContextAsync(context, _cancelTrigger);
        var cancelBookmark = cancelTriggerContext.CreateBookmark("cancel");

        await CompleteScheduledActivityAsync(context, _payTrigger);

        await Assert.That(cancelTriggerContext.Status).IsEqualTo(ActivityStatus.Canceled);
        await Assert.That(context.WorkflowExecutionContext.Bookmarks).DoesNotContain(cancelBookmark);
    }

    [Test]
    [DisplayName("StateMachine cancels every distinct competing outbound trigger")]
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

        await Assert.That(cancelTriggerContext.Status).IsEqualTo(ActivityStatus.Canceled);
        await Assert.That(thirdTriggerContext.Status).IsEqualTo(ActivityStatus.Canceled);
        await Assert.That(context.WorkflowExecutionContext.Bookmarks).DoesNotContain(cancelBookmark);
        await Assert.That(context.WorkflowExecutionContext.Bookmarks).DoesNotContain(thirdBookmark);
    }

    [Test]
    [DisplayName("StateMachine rejects transitions that share a trigger instance")]
    public async Task SharedTriggerInstanceIsRejected()
    {
        _stateMachine.Transitions.Single(x => x.Name == "Cancel").Trigger = _payTrigger;

        var exception = (await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => ExecuteAsync()))!;

        await Assert.That(exception.Message).Contains("cannot share a Trigger activity");
    }

    [Test]
    [DisplayName("StateMachine rejects transition triggers with duplicate activity IDs")]
    public async Task DuplicateTriggerIdIsRejected()
    {
        _cancelTrigger.Id = _payTrigger.Id;

        var exception = (await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => ExecuteAsync()))!;

        await Assert.That(exception.Message).Contains("unique ID");
    }

    [Test]
    [DisplayName("StateMachine removes re-armed competing triggers when a different transition wins")]
    public async Task AcceptedTransitionRemovesRearmedCompetingTriggers()
    {
        var context = await ExecuteAndEnterNewStateAsync();
        _stateMachine.Transitions.Single(x => x.Name == "Pay").Condition = new(false);

        await CompleteScheduledActivityAsync(context, _payTrigger);
        await Assert.That(context.WorkflowExecutionContext.Scheduler.Any(x => x.Activity == _payTrigger)).IsTrue();
        await Assert.That(context.WorkflowExecutionContext.CompletionCallbacks).Contains(x => x.Owner == context && x.Child.Activity == _payTrigger);

        await CompleteScheduledActivityAsync(context, _cancelTrigger);

        await Assert.That(context.WorkflowExecutionContext.Scheduler.Any(x => x.Activity == _payTrigger)).IsFalse();
        await Assert.That(context.WorkflowExecutionContext.CompletionCallbacks).DoesNotContain(x => x.Owner == context && x.Child.Activity == _payTrigger);
    }

    [Test]
    [DisplayName("StateMachine stores current state in the activity execution context")]
    public async Task StoresCurrentStateInActivityExecutionContext()
    {
        var payTransition = _stateMachine.Transitions.Single(x => x.Name == "Pay");
        payTransition.Action = null;
        _stateMachine.States.Single(x => x.Name == "New").Exit = null;
        _stateMachine.States.Single(x => x.Name == "Paid").Entry = null;
        var context = await ExecuteAndEnterNewStateAsync();

        await CompleteScheduledActivityAsync(context, _payTrigger);

        await Assert.That(context.GetProperty<string>(CurrentStateProperty)).IsEqualTo("Paid");
    }

    [Test]
    [DisplayName("StateMachine resolves duplicate unnamed transition endpoints by scheduled transition")]
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

        await Assert.That(context.HasScheduledActivity(firstAction)).IsFalse();
        await Assert.That(context.HasScheduledActivity(secondAction)).IsTrue();
    }

    [Test]
    [DisplayName("StateMachine resolves duplicate named transitions by current state")]
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

        await Assert.That(context.GetProperty<string>(CurrentStateProperty)).IsEqualTo("Closed");
    }

    [Test]
    [DisplayName("StateMachine ignores stale transition action completions")]
    public async Task IgnoresStaleTransitionActionCompletions()
    {
        var context = await ExecuteAndEnterNewStateAsync();

        await CompleteScheduledActivityAsync(context, _payTrigger);
        await CompleteScheduledActivityAsync(context, _newExit);
        context.SetProperty(CurrentStateProperty, "Paid");
        await CompleteScheduledActivityAsync(context, _payAction);

        await Assert.That(context.HasScheduledActivity(_paidEntry)).IsFalse();
    }

    [Test]
    [DisplayName("StateMachine ignores stale state exit completions")]
    public async Task IgnoresStaleStateExitCompletions()
    {
        var context = await ExecuteAndEnterNewStateAsync();

        await CompleteScheduledActivityAsync(context, _payTrigger);
        context.SetProperty(CurrentStateProperty, "Paid");
        await CompleteScheduledActivityAsync(context, _newExit);

        await Assert.That(context.HasScheduledActivity(_payAction)).IsFalse();
    }

    private async Task<ActivityExecutionContext> ExecuteAndEnterNewStateAsync()
    {
        var context = await ExecuteAsync();
        await CompleteScheduledActivityAsync(context, _newEntry);
        return context;
    }

    private static ActivityTestFixture CreateFixture(StateMachineActivity stateMachine) => new ActivityTestFixture(stateMachine)
        .ConfigureServices(services =>
        {
            services.RemoveAll<IWorkflowExecutionContextSchedulerStrategy>();
            services.AddSingleton<IWorkflowExecutionContextSchedulerStrategy, WorkflowExecutionContextSchedulerStrategy>();
        });

    private static ActivityTestFixture CreatePersistenceFixture(StateMachineActivity stateMachine) => CreateFixture(stateMachine)
        .ConfigureServices(services =>
        {
            services.RemoveAll<IActivityExecutionContextSchedulerStrategy>();
            services.AddSingleton<IActivityExecutionContextSchedulerStrategy, ActivityExecutionContextSchedulerStrategy>();
            services.AddSingleton<IMediator>(_ => Substitute.For<IMediator>());
            services.AddSingleton<IVariablePersistenceManager>(_ => Substitute.For<IVariablePersistenceManager>());
        });

    private Task<ActivityExecutionContext> ExecuteAsync(StateMachineActivity? stateMachine = null) => CreateFixture(stateMachine ?? _stateMachine).ExecuteAsync();

    private static async Task CompleteScheduledActivityAsync(ActivityExecutionContext ownerContext, IActivity activity)
    {
        var childContext = await CreateScheduledActivityContextAsync(ownerContext, activity);
        var callback = PopCallback(ownerContext, activity);

        await Assert.That(callback?.CompletionCallback is not null).IsTrue();
        ownerContext.Tag = callback!.Tag;
        await callback.CompletionCallback!(new ActivityCompletedContext(ownerContext, childContext));
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

    private static async Task ExecuteNextScheduledActivityAsync(WorkflowExecutionContext workflowContext)
    {
        var workItem = workflowContext.Scheduler.Take();
        var childContext = await workflowContext.CreateActivityExecutionContextAsync(workItem.Activity, new ActivityInvocationOptions
        {
            Owner = workItem.Owner,
            ExistingActivityExecutionContext = workItem.ExistingActivityExecutionContext,
            Tag = workItem.Tag,
            Variables = workItem.Variables,
            Input = workItem.Input,
            SchedulingActivityExecutionId = workItem.SchedulingActivityExecutionId,
            SchedulingWorkflowInstanceId = workItem.SchedulingWorkflowInstanceId,
            SchedulingCallStackDepth = workItem.SchedulingCallStackDepth
        });
        childContext.TransitionTo(ActivityStatus.Running);
        workflowContext.AddActivityExecutionContext(childContext);
        await childContext.Activity.ExecuteAsync(childContext);
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
