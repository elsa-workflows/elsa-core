using System.Linq.Expressions;
using Elsa.Expressions.Services;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.Signals;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Core;

public static class ActivityExecutionContextExtensions
{
    public static bool TryGetInput<T>(this ActivityExecutionContext context, string key, out T value) => context.Input!.TryGetValue(key, out value!);
    public static T GetInput<T>(this ActivityExecutionContext context) => context.GetInput<T>(typeof(T).Name);
    public static T GetInput<T>(this ActivityExecutionContext context, string key) => (T)context.Input[key];

    public static WorkflowExecutionLogEntry AddExecutionLogEntry(this ActivityExecutionContext context, string eventName, string? message = default, string? source = default, object? payload = default)
    {
        var activity = context.Activity;
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var now = context.GetRequiredService<ISystemClock>().UtcNow;
        var logEntry = new WorkflowExecutionLogEntry(activity.Id, activity.TypeName, now, eventName, message, source, payload);
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
        var assignedInputs = inputs.Where(x => x.MemoryReference != null!).ToList();
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var expressionExecutionContext = context.ExpressionExecutionContext;

        foreach (var input in assignedInputs)
        {
            var locationReference = input.MemoryReference;
            var value = await evaluator.EvaluateAsync(input, expressionExecutionContext);
            locationReference.Set(context, value);
        }
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

        if (input.MemoryReference == null!)
            throw new Exception("Input not initialized");

        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var expressionExecutionContext = context.ExpressionExecutionContext;

        var locationReference = input.MemoryReference;
        var value = await evaluator.EvaluateAsync(input, expressionExecutionContext);
        locationReference.Set(context, value);

        return input;
    }

    public static async Task<T?> EvaluateAsync<T>(this ActivityExecutionContext context, Input<T> input)
    {
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var locationReference = input.MemoryReference;
        var value = await evaluator.EvaluateAsync(input, context.ExpressionExecutionContext);
        locationReference.Set(context, value);
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
    public static async ValueTask SignalAsync(this ActivityExecutionContext context, object signal)
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
    public static async ValueTask CompleteActivityAsync(this ActivityExecutionContext context)
    {
        // Send a signal.
        await context.SignalAsync(new ActivityCompleted());

        // Remove the context.
        context.WorkflowExecutionContext.ActivityExecutionContexts.Remove(context);
    }

    public static ILogger GetLogger(this ActivityExecutionContext context) => (ILogger)context.GetRequiredService(typeof(ILogger<>).MakeGenericType(context.Activity.GetType()));
}