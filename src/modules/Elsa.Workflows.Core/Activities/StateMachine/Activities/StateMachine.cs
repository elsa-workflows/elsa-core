using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Workflows.Activities.StateMachine.Models;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using JetBrains.Annotations;

namespace Elsa.Workflows.Activities.StateMachine.Activities;

/// <summary>
/// Executes a state machine made of named states and trigger-driven transitions.
/// </summary>
[Activity("Elsa", "Flow", "Executes a state machine made of named states and trigger-driven transitions.")]
[PublicAPI]
public class StateMachine : Activity
{
    private const string PhaseEntering = "Entering";
    private const string CurrentStateProperty = "CurrentState";

    /// <inheritdoc />
    public StateMachine([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The states in declaration order.
    /// </summary>
    public ICollection<StateMachineState> States { get; set; } = new List<StateMachineState>();

    /// <summary>
    /// The transitions in declaration order.
    /// </summary>
    public ICollection<Transition> Transitions { get; set; } = new List<Transition>();

    /// <summary>
    /// The first state to enter when no current state is set.
    /// </summary>
    public string? InitialState { get; set; }

    /// <summary>
    /// The currently active state.
    /// </summary>
    public string? CurrentState { get; set; }

    /// <summary>
    /// Exposes nested activities to the workflow graph builder.
    /// </summary>
    [JsonIgnore]
    [Browsable(false)]
    public IEnumerable<IActivity> Activities =>
        States.SelectMany(x => new[] { x.Entry, x.Exit })
            .Concat(Transitions.SelectMany(x => new[] { x.Trigger, x.Action }))
            .Where(x => x != null)
            .Cast<IActivity>();

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var currentState = GetCurrentState(context);
        SetCurrentState(context, string.IsNullOrWhiteSpace(currentState) ? InitialState : currentState);

        if (FindState(GetCurrentState(context)) == null)
        {
            await context.CompleteActivityAsync();
            return;
        }

        await EnterStateAsync(context);
    }

    private async ValueTask EnterStateAsync(ActivityExecutionContext context, ActivityExecutionContext? schedulingContext = null)
    {
        var state = FindState(GetCurrentState(context));

        if (state == null)
        {
            await context.CompleteActivityAsync();
            return;
        }

        if (state.Entry != null)
        {
            await ScheduleAsync(context, state.Entry, OnStateEntryCompletedAsync, PhaseEntering, schedulingContext);
            return;
        }

        await ScheduleOutboundTriggersAsync(context, schedulingContext);
    }

    private async ValueTask OnStateEntryCompletedAsync(ActivityCompletedContext context)
    {
        await ScheduleOutboundTriggersAsync(context.TargetContext, context.ChildContext);
    }

    private async ValueTask ScheduleOutboundTriggersAsync(ActivityExecutionContext context, ActivityExecutionContext? schedulingContext = null)
    {
        var outboundTransitions = GetOutboundTransitions(GetCurrentState(context)).Where(x => x.Trigger != null && FindState(x.To) != null).ToList();

        if (!outboundTransitions.Any())
        {
            await context.CompleteActivityAsync();
            return;
        }

        foreach (var transition in outboundTransitions)
            await ScheduleAsync(context, transition.Trigger!, OnTriggerCompletedAsync, GetTransitionKey(transition), schedulingContext);
    }

    private async ValueTask OnTriggerCompletedAsync(ActivityCompletedContext context)
    {
        var targetContext = context.TargetContext;
        var transition = FindTransitionByKey(targetContext, context.ChildContext.Tag as string) ?? FindTransitionByTrigger(targetContext, context.ChildContext.Activity);

        if (transition == null || !IsCurrentSource(targetContext, transition) || FindState(transition.To) == null)
            return;

        var canTransition = transition.Condition == null || await EvaluateConditionAsync(targetContext, transition.Condition);

        if (!canTransition)
        {
            if (transition.Trigger != null)
                await ScheduleAsync(targetContext, transition.Trigger, OnTriggerCompletedAsync, GetTransitionKey(transition), context.ChildContext);

            return;
        }

        await CancelCompetingTriggersAsync(targetContext, transition, context.ChildContext);

        if (transition.Action != null)
        {
            await ScheduleTransitionActivityAsync(targetContext, transition.Action, transition, OnTransitionActionCompletedAsync, context.ChildContext);
            return;
        }

        await ExitStateAsync(targetContext, transition, context.ChildContext);
    }

    private async ValueTask OnTransitionActionCompletedAsync(ActivityCompletedContext context)
    {
        var targetContext = context.TargetContext;
        var transition = FindTransitionByKey(targetContext, context.ChildContext.Tag as string);

        if (transition == null || !IsCurrentSource(targetContext, transition))
            return;

        await ExitStateAsync(targetContext, transition, context.ChildContext);
    }

    private async ValueTask ExitStateAsync(ActivityExecutionContext context, Transition transition, ActivityExecutionContext? schedulingContext = null)
    {
        var sourceState = FindState(transition.From);

        if (sourceState?.Exit != null)
        {
            await ScheduleTransitionActivityAsync(context, sourceState.Exit, transition, OnStateExitCompletedAsync, schedulingContext);
            return;
        }

        await CompleteTransitionAsync(context, transition, schedulingContext);
    }

    private async ValueTask OnStateExitCompletedAsync(ActivityCompletedContext context)
    {
        var targetContext = context.TargetContext;
        var transition = FindTransitionByKey(targetContext, context.ChildContext.Tag as string);

        if (transition == null || !IsCurrentSource(targetContext, transition))
            return;

        await CompleteTransitionAsync(targetContext, transition, context.ChildContext);
    }

    private async ValueTask CompleteTransitionAsync(ActivityExecutionContext context, Transition transition, ActivityExecutionContext? schedulingContext = null)
    {
        SetCurrentState(context, transition.To);
        await EnterStateAsync(context, schedulingContext);
    }

    private async ValueTask ScheduleTransitionActivityAsync(
        ActivityExecutionContext context,
        IActivity activity,
        Transition transition,
        ActivityCompletionCallback callback,
        ActivityExecutionContext? schedulingContext)
    {
        await ScheduleAsync(context, activity, callback, GetTransitionKey(transition), schedulingContext);
    }

    private static async ValueTask ScheduleAsync(
        ActivityExecutionContext context,
        IActivity activity,
        ActivityCompletionCallback callback,
        string tag,
        ActivityExecutionContext? schedulingContext)
    {
        var options = new ScheduleWorkOptions
        {
            CompletionCallback = callback,
            Tag = tag,
            SchedulingActivityExecutionId = schedulingContext?.Id
        };

        await context.ScheduleActivityAsync(activity, options);
    }

    private async Task CancelCompetingTriggersAsync(ActivityExecutionContext context, Transition winningTransition, ActivityExecutionContext winningTriggerContext)
    {
        var competingTriggerIds = GetOutboundTransitions(winningTransition.From)
            .Where(x => !ReferenceEquals(x, winningTransition))
            .Select(x => x.Trigger?.Id)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet();

        var competingTriggerContexts = context.WorkflowExecutionContext.ActivityExecutionContexts
            .Where(x => !ReferenceEquals(x, winningTriggerContext) && competingTriggerIds.Contains(x.Activity.Id))
            .ToList();

        foreach (var competingTriggerContext in competingTriggerContexts)
            await competingTriggerContext.CancelActivityAsync();
    }

    private string? GetCurrentState(ActivityExecutionContext context) => context.GetProperty<string>(CurrentStateProperty) ?? CurrentState;

    private void SetCurrentState(ActivityExecutionContext context, string? state)
    {
        CurrentState = state;

        if (state == null)
            context.RemoveProperty(CurrentStateProperty);
        else
            context.SetProperty(CurrentStateProperty, state);
    }

    private bool IsCurrentSource(ActivityExecutionContext context, Transition transition) => string.Equals(transition.From, GetCurrentState(context), StringComparison.Ordinal);

    private static async Task<bool> EvaluateConditionAsync(ActivityExecutionContext context, Input<bool> condition)
    {
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        return await evaluator.EvaluateAsync(condition, context.ExpressionExecutionContext);
    }

    private StateMachineState? FindState(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? null
            : States.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.Ordinal));

    private IEnumerable<Transition> GetOutboundTransitions(string? sourceState) =>
        string.IsNullOrWhiteSpace(sourceState)
            ? []
            : Transitions.Where(x => string.Equals(x.From, sourceState, StringComparison.Ordinal));

    private Transition? FindTransitionByTrigger(ActivityExecutionContext context, IActivity trigger) =>
        GetOutboundTransitions(GetCurrentState(context)).FirstOrDefault(x => ReferenceEquals(x.Trigger, trigger));

    private Transition? FindTransitionByKey(ActivityExecutionContext context, string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var currentState = GetCurrentState(context);
        return GetOutboundTransitions(currentState).FirstOrDefault(x => string.Equals(GetTransitionKey(x), key, StringComparison.Ordinal))
               ?? Transitions.FirstOrDefault(x => string.Equals(GetTransitionKey(x), key, StringComparison.Ordinal));
    }

    private string GetTransitionKey(Transition transition)
    {
        var index = 0;
        foreach (var current in Transitions)
        {
            if (ReferenceEquals(current, transition))
                return $"{index}:{GetTransitionDisplayKey(transition)}";

            index++;
        }

        return GetTransitionDisplayKey(transition);
    }

    private static string GetTransitionDisplayKey(Transition transition) =>
        string.IsNullOrWhiteSpace(transition.Name)
            ? $"{transition.From}->{transition.To}"
            : transition.Name;
}
