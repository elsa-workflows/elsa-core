using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Signals;

namespace Elsa.Workflows.Activities.StateMachine;

/// <summary>
/// Executes a workflow as a state machine.
/// </summary>
[Activity("Elsa", "Flow", "Execute a workflow as a state machine.")]
public class StateMachine : Activity
{
    /// <inheritdoc />
    public StateMachine([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The states in this state machine.
    /// </summary>
    public ICollection<State> States { get; set; } = new List<State>();

    /// <summary>
    /// The name of the initial state.
    /// </summary>
    public string? InitialState { get; set; }

    /// <summary>
    /// The name of the currently active state.
    /// </summary>
    public string? CurrentState { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var stateName = CurrentState ?? InitialState;
        if (stateName == null)
        {
            await context.CompleteActivityAsync();
            return;
        }

        var state = FindState(stateName);
        if (state == null)
        {
            await context.CompleteActivityAsync();
            return;
        }

        CurrentState = state.Name;
        await EnterStateAsync(context, state);
    }

    private readonly Dictionary<Transition, ActivityExecutionContext> _triggerContexts = new();

    private State? FindState(string? name) => name == null ? null : States.FirstOrDefault(x => x.Name == name);

    private async ValueTask EnterStateAsync(ActivityExecutionContext context, State state)
    {
        await CancelScheduledTriggersAsync();

        if (state.Entry != null)
            await context.ScheduleActivityAsync(state.Entry);

        foreach (var transition in state.Transitions)
        {
            if (transition.Trigger == null)
                continue;

            var triggerContext = await context.WorkflowExecutionContext.CreateActivityExecutionContextAsync(transition.Trigger);
            _triggerContexts[transition] = triggerContext;
            await context.ScheduleActivityAsync(triggerContext.Activity, new()
            {
                CompletionCallback = ctx => OnTriggerCompleted(ctx, transition),
                ExistingActivityExecutionContext = triggerContext
            });
        }

        if (!state.Transitions.Any())
            await context.CompleteActivityAsync();
    }

    private async ValueTask OnTriggerCompleted(ActivityCompletedContext context, Transition transition)
    {
        var stateMachineContext = context.TargetContext;
        var condition = transition.Condition == null || stateMachineContext.Get(transition.Condition);

        if (!condition)
            return;

        await CancelScheduledTriggersAsync(transition);

        if (transition.Action != null)
        {
            await stateMachineContext.ScheduleActivityAsync(transition.Action, ctx => OnTransitionActionCompleted(ctx, transition));
        }
        else
        {
            await OnTransitionActionCompleted(context, transition);
        }
    }

    private async ValueTask OnTransitionActionCompleted(ActivityCompletedContext context, Transition transition)
    {
        var executionContext = context.TargetContext;
        var fromState = FindState(CurrentState);
        var toState = FindState(transition.To);

        if (fromState?.Exit != null)
            await executionContext.ScheduleActivityAsync(fromState.Exit);

        CurrentState = transition.To;

        if (toState != null)
            await EnterStateAsync(executionContext, toState);
        else
            await executionContext.CompleteActivityAsync();
    }

    private async ValueTask CancelScheduledTriggersAsync(Transition? except = null)
    {
        foreach (var (trans, triggerContext) in _triggerContexts.ToList())
        {
            if (except != null && ReferenceEquals(trans, except))
                continue;

            await triggerContext.CancelActivityAsync();
            _triggerContexts.Remove(trans);
        }
    }
}
