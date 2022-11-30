using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Elsa.Common.Services;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Services;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.Signals;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Core;

public static class ActivityExecutionContextExtensions
{
    public static bool TryGetInput<T>(this ActivityExecutionContext context, string key, out T value, JsonSerializerOptions? serializerOptions = default)
    {
        if (context.Input.TryGetValue(key, out var v))
        {
            value = v.ConvertTo<T>(serializerOptions)!;
            return true;
        }

        value = default!;
        return false;
    }

    public static T GetInput<T>(this ActivityExecutionContext context, JsonSerializerOptions? serializerOptions = default) => context.GetInput<T>(typeof(T).Name, serializerOptions);
    public static T GetInput<T>(this ActivityExecutionContext context, string key, JsonSerializerOptions? serializerOptions = default) => context.Input[key].ConvertTo<T>(serializerOptions)!;

    /// <summary>
    /// Returns true if this activity is triggered for the first time and not being resumed.
    /// </summary>
    public static bool IsTriggerOfWorkflow(this ActivityExecutionContext context) => context.WorkflowExecutionContext.TriggerActivityId == context.Activity.Id;

    public static WorkflowExecutionLogEntry AddExecutionLogEntry(this ActivityExecutionContext context, string eventName, string? message = default, string? source = default, object? payload = default)
    {
        var activity = context.Activity;
        var activityInstanceId = context.Id;
        var parentActivityInstanceId = context.ParentActivityExecutionContext?.Id;
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var now = context.GetRequiredService<ISystemClock>().UtcNow;
        var logEntry = new WorkflowExecutionLogEntry(activityInstanceId, parentActivityInstanceId, activity.Id, activity.Type, now, eventName, message, source, payload);
        workflowExecutionContext.ExecutionLog.Add(logEntry);
        return logEntry;
    }

    public static Variable SetVariable(this ActivityExecutionContext context, string name, object? value) => context.ExpressionExecutionContext.SetVariable(name, value);
    public static T? GetVariable<T>(this ActivityExecutionContext context, string name) => context.ExpressionExecutionContext.GetVariable<T?>(name);

    /// <summary>
    /// Evaluates each input property of the activity.
    /// </summary>
    public static async Task EvaluateInputPropertiesAsync(this ActivityExecutionContext context)
    {
        var activity = context.Activity;
        var inputs = activity.GetInputs();
        var assignedInputs = inputs.Where(x => x.MemoryBlockReference != null!).ToList();
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var expressionExecutionContext = context.ExpressionExecutionContext;

        foreach (var input in assignedInputs)
        {
            var memoryReference = input.MemoryBlockReference();
            var value = await evaluator.EvaluateAsync(input, expressionExecutionContext);
            memoryReference.Set(context, value);
        }

        context.SetHasEvaluatedProperties();
    }

    public static async Task<T?> EvaluateInputPropertyAsync<TActivity, T>(this ActivityExecutionContext context, Expression<Func<TActivity, Input<T>>> propertyExpression)
    {
        var inputName = propertyExpression.GetProperty()!.Name;
        var input = await EvaluateInputPropertyAsync(context, inputName);
        return context.Get((Input<T>)input);
    }

    /// <summary>
    /// Evaluates a specific input property of the activity.
    /// </summary>
    public static async Task<Input> EvaluateInputPropertyAsync(this ActivityExecutionContext context, string inputName)
    {
        var activity = context.Activity;
        var input = activity.GetInput(inputName);

        if (input == null)
            throw new Exception($"No input with name {inputName} could be found");

        if (input.MemoryBlockReference == null!)
            throw new Exception("Input not initialized");

        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var expressionExecutionContext = context.ExpressionExecutionContext;

        var memoryBlockReference = input.MemoryBlockReference();
        var value = await evaluator.EvaluateAsync(input, expressionExecutionContext);
        memoryBlockReference.Set(context, value);

        return input;
    }
    
    /// <summary>
    /// Schedules the specified activity with the provided callback.
    /// If the activity is null, the callback is invoked immediately.
    /// </summary>
    public static async Task ScheduleOutcomeAsync(this ActivityExecutionContext context, IActivity? activity, [CallerArgumentExpression("activity")] string portPropertyName = default!)
    {
        if (activity == null)
        {
            var outcome = context.GetOutcomeName(portPropertyName);
            await context.CompleteActivityWithOutcomesAsync(outcome);
            return;
        }

        await context.ScheduleActivityAsync(activity, context);
    }

    public static string GetOutcomeName(this ActivityExecutionContext context, string portPropertyName)
    {
        var owner = context.Activity;
        var ports = owner.GetType().GetProperties().Where(x => typeof(IActivity).IsAssignableFrom(x.PropertyType)).ToList();

        var portQuery =
            from p in ports
            where p.Name == portPropertyName
            select p;

        var portProperty = portQuery.First();
        return portProperty.GetCustomAttribute<PortAttribute>()?.Name ?? portProperty.Name;
    }

    public static async Task<T?> EvaluateAsync<T>(this ActivityExecutionContext context, Input<T> input)
    {
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var memoryBlockReference = input.MemoryBlockReference();
        var value = await evaluator.EvaluateAsync(input, context.ExpressionExecutionContext);
        memoryBlockReference.Set(context, value);
        return value;
    }

    /// <summary>
    /// Returns a flattened list of the current context's ancestors.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<ActivityExecutionContext> GetAncestors(this ActivityExecutionContext context)
    {
        var current = context.ParentActivityExecutionContext;

        while (current != null)
        {
            yield return current;
            current = current.ParentActivityExecutionContext;
        }
    }

    /// <summary>
    /// Returns a flattened list of the current context's immediate children.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<ActivityExecutionContext> GetChildren(this ActivityExecutionContext context) =>
        context.WorkflowExecutionContext.ActivityExecutionContexts.Where(x => x.ParentActivityExecutionContext == context);

    /// <summary>
    /// Removes all child <see cref="ActivityExecutionContext"/> objects.
    /// </summary>
    public static void RemoveChildren(this ActivityExecutionContext context)
    {
        // Detach child activity execution contexts.
        context.WorkflowExecutionContext.RemoveActivityExecutionContexts(context.GetChildren());
    }

    /// <summary>
    /// Send a signal up the current branch.
    /// </summary>
    public static async ValueTask SendSignalAsync(this ActivityExecutionContext context, object signal)
    {
        var ancestorContexts = new[] { context }.Concat(context.GetAncestors());

        foreach (var ancestorContext in ancestorContexts)
        {
            var signalContext = new SignalContext(ancestorContext, context, context.CancellationToken);

            if (ancestorContext.Activity is not ISignalHandler handler)
                continue;

            await handler.HandleSignalAsync(signal, signalContext);

            if (signalContext.StopPropagationRequested)
                return;
        }
    }

    /// <summary>
    /// Complete the current activity. This should only be called by activities that explicitly suppress automatic-completion.
    /// </summary>
    public static async ValueTask CompleteActivityAsync(this ActivityExecutionContext context, object? result = default)
    {
        // Send a signal.
        await context.SendSignalAsync(new ActivityCompleted(result));

        // Remove the context.
        context.WorkflowExecutionContext.RemoveActivityExecutionContext(context);
    }

    /// <summary>
    /// Complete the current activity with the specified outcome.
    /// </summary>
    public static ValueTask CompleteActivityWithOutcomesAsync(this ActivityExecutionContext context, params string[] outcomes) => context.CompleteActivityAsync(new Outcomes(outcomes));
    
    /// <summary>
    /// Complete the current composite activity with the specified outcome.
    /// </summary>
    public static async ValueTask CompleteCompositeAsync(this ActivityExecutionContext context, params string[] outcomes) => await context.SendSignalAsync(new CompleteCompositeSignal(new Outcomes(outcomes)));

    /// <summary>
    /// Cancel the activity. For blocking activities, it means their bookmarks will be removed. For job activities, the background work will be cancelled.
    /// </summary>
    public static async Task CancelActivityAsync(this ActivityExecutionContext context)
    {
        context.ClearBookmarks();
        await context.SendSignalAsync(new CancelSignal());
    }

    public static ILogger GetLogger(this ActivityExecutionContext context) => (ILogger)context.GetRequiredService(typeof(ILogger<>).MakeGenericType(context.Activity.GetType()));

    internal static bool GetHasEvaluatedProperties(this ActivityExecutionContext context) => context.TransientProperties.TryGetValue<bool>("HasEvaluatedProperties", out var value) && value;
    internal static void SetHasEvaluatedProperties(this ActivityExecutionContext context) => context.TransientProperties["HasEvaluatedProperties"] = true;
}