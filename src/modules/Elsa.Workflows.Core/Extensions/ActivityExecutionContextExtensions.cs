using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Elsa.Common.Contracts;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Notifications;
using Elsa.Workflows.Core.Signals;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ActivityExecutionContext"/>.
/// </summary>
[PublicAPI]
public static class ActivityExecutionContextExtensions
{
    /// <summary>
    /// Attempts to get a value from the input provided via <see cref="WorkflowExecutionContext"/>. If a value was found, an attempt is made to convert it into the specified type <code>T</code>.
    /// </summary>
    public static bool TryGetInput<T>(this ActivityExecutionContext context, string key, out T value, JsonSerializerOptions? serializerOptions = default)
    {
        var wellKnownTypeRegistry = context.GetRequiredService<IWellKnownTypeRegistry>();

        if (context.Input.TryGetValue(key, out var v))
        {
            value = v.ConvertTo<T>(new ObjectConverterOptions(serializerOptions, wellKnownTypeRegistry))!;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Gets a value from the input provided via <see cref="WorkflowExecutionContext"/>. If a value was found, an attempt is made to convert it into the specified type <code>T</code>.
    /// </summary>
    public static T GetInput<T>(this ActivityExecutionContext context, JsonSerializerOptions? serializerOptions = default) => context.GetInput<T>(typeof(T).Name, serializerOptions);

    /// <summary>
    /// Gets a value from the input provided via <see cref="WorkflowExecutionContext"/>. If a value was found, an attempt is made to convert it into the specified type <code>T</code>.
    /// </summary>
    public static T GetInput<T>(this ActivityExecutionContext context, string key, JsonSerializerOptions? serializerOptions = default)
    {
        var wellKnownTypeRegistry = context.GetRequiredService<IWellKnownTypeRegistry>();
        return context.Input[key].ConvertTo<T>(new ObjectConverterOptions(serializerOptions, wellKnownTypeRegistry))!;
    }
    
    /// <summary>
    /// Sets the Result property of the specified activity.
    /// </summary>
    /// <param name="context">The <see cref="ActivityExecutionContext"/></param> being extended.
    /// <param name="value">The value to set.</param>
    /// <exception cref="Exception">Thrown when the specified activity does not implement <see cref="IActivityWithResult"/>.</exception>
    public static void SetResult(this ActivityExecutionContext context, object? value)
    {
        var activity = context.Activity as IActivityWithResult ?? throw new Exception($"Cannot set result on activity {context.Activity.Id} because it does not implement {nameof(IActivityWithResult)}.");
        context.Set(activity.Result, value);
    }

    /// <summary>
    /// Returns true if this activity is triggered for the first time and not being resumed.
    /// </summary>
    public static bool IsTriggerOfWorkflow(this ActivityExecutionContext context) => context.WorkflowExecutionContext.TriggerActivityId == context.Activity.Id;

    /// <summary>
    /// Adds a new <see cref="WorkflowExecutionLogEntry"/> to the execution log of the current <see cref="WorkflowExecutionContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="ActivityExecutionContext"/></param> being extended.
    /// <param name="eventName">The name of the event.</param>
    /// <param name="message">The message of the event.</param>
    /// <param name="source">The source of the activity. For example, the source file name and line number in case of composite activities.</param>
    /// <param name="payload">Any contextual data related to this event.</param>
    /// <param name="includeActivityState">True to include activity state with this event, false otherwise.</param>
    /// <returns>Returns the created <see cref="WorkflowExecutionLogEntry"/>.</returns>
    public static WorkflowExecutionLogEntry AddExecutionLogEntry(this ActivityExecutionContext context, string eventName, string? message = default, string? source = default, object? payload = default, bool includeActivityState = false)
    {
        var activity = context.Activity;
        var activityInstanceId = context.Id;
        var parentActivityInstanceId = context.ParentActivityExecutionContext?.Id;
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var now = context.GetRequiredService<ISystemClock>().UtcNow;
        var activityState = includeActivityState ? context.ActivityState : default;

        var logEntry = new WorkflowExecutionLogEntry(
            activityInstanceId,
            parentActivityInstanceId,
            activity.Id,
            activity.Type,
            activity.Version,
            activity.Name,
            context.NodeId,
            activityState,
            now,
            workflowExecutionContext.ExecutionLogSequence++,
            eventName,
            message,
            source ?? activity.GetSource(),
            payload);

        workflowExecutionContext.ExecutionLog.Add(logEntry);
        return logEntry;
    }

    /// <summary>
    /// Sets a workflow variable by name.
    /// </summary>
    /// <param name="context">The <see cref="ActivityExecutionContext"/> being extended.</param>
    /// <param name="name">The name of the variable.</param>
    /// <param name="value">The value of the variable.</param>
    /// <param name="storageDriverType">The type of the storage driver to use.</param>
    /// <param name="configure">A callback to configure the memory block.</param>
    /// <returns>The created <see cref="Variable"/>.</returns>
    public static Variable SetVariable(this ActivityExecutionContext context, string name, object? value, Type? storageDriverType = default, Action<MemoryBlock>? configure = default) =>
        context.ExpressionExecutionContext.SetVariable(name, value, storageDriverType, configure);

    /// <summary>
    /// Sets a workflow variable by name.
    /// </summary>
    /// <param name="context">The <see cref="ActivityExecutionContext"/> being extended.</param>
    /// <param name="name">The name of the variable.</param>
    /// <typeparam name="T">The type of the variable.</typeparam>
    /// <returns>The variable if found, otherwise null.</returns>
    public static T? GetVariable<T>(this ActivityExecutionContext context, string name) => context.ExpressionExecutionContext.GetVariable<T?>(name);

    /// <summary>
    /// Returns a dictionary of variable keys and their values across scopes.
    /// </summary>
    public static IDictionary<string, object> GetVariableValues(this ActivityExecutionContext activityExecutionContext) => activityExecutionContext.ExpressionExecutionContext.ReadAndFlattenMemoryBlocks();

    /// <summary>
    /// Evaluates each input property of the activity.
    /// </summary>
    public static async Task EvaluateInputPropertiesAsync(this ActivityExecutionContext context)
    {
        var activity = context.Activity;
        var activityDescriptor = context.ActivityDescriptor;
        var wrappedInputs = activityDescriptor.GetWrappedInputPropertyDescriptors(activity);

        foreach (var inputDescriptor in wrappedInputs) 
            await EvaluateInputPropertyAsync(context, activityDescriptor, inputDescriptor);

        context.SetHasEvaluatedProperties();
    }

    /// <summary>
    /// Evaluates the specified input property of the activity.
    /// </summary>
    public static async Task<T?> EvaluateInputPropertyAsync<TActivity, T>(this ActivityExecutionContext context, Expression<Func<TActivity, Input<T>>> propertyExpression)
    {
        var inputName = propertyExpression.GetProperty()!.Name;
        var input = await EvaluateInputPropertyAsync(context, inputName);
        return input != null ? context.Get((Input<T>)input) : default;
    }

    /// <summary>
    /// Evaluates a specific input property of the activity.
    /// </summary>
    public static async Task<Input?> EvaluateInputPropertyAsync(this ActivityExecutionContext context, string inputName)
    {
        var activity = context.Activity;
        var activityRegistry = context.GetRequiredService<IActivityRegistry>();
        var activityDescriptor = activityRegistry.Find(activity.Type) ?? throw new Exception("Activity descriptor not found");
        var inputDescriptor = activityDescriptor.GetWrappedInputPropertyDescriptor(activity, inputName);

        if (inputDescriptor == null)
            throw new Exception($"No input with name {inputName} could be found");

        return await EvaluateInputPropertyAsync(context, activityDescriptor, inputDescriptor);
    }
    
    private static async Task<Input?> EvaluateInputPropertyAsync(this ActivityExecutionContext context, ActivityDescriptor activityDescriptor, InputDescriptor inputDescriptor)
    {
        var activity = context.Activity;
        var defaultValue = inputDescriptor.DefaultValue;
        var value = defaultValue;
        var input = inputDescriptor.ValueGetter(activity) as Input;

        if (defaultValue != null && input == null)
        {
            var typedInput = typeof(Input<>).MakeGenericType(inputDescriptor.Type);
            var valueExpression = new Literal(defaultValue)
            {
                Id = Guid.NewGuid().ToString()
            };
            input = (Input)Activator.CreateInstance(typedInput, valueExpression)!;
            inputDescriptor.ValueSetter(activity, input);
        }
        else
        {
            var evaluator = context.GetRequiredService<IExpressionEvaluator>();
            var expressionExecutionContext = context.ExpressionExecutionContext;
            value = input?.Expression != null ? await evaluator.EvaluateAsync(input, expressionExecutionContext) : defaultValue;
        }
            
        var memoryReference = input?.MemoryBlockReference();
        
        // When input is created from an activity provider, there may be no memory block reference.
        if(memoryReference?.Id != null!)
        {
            // Declare the input memory block on the current context. 
            context.ExpressionExecutionContext.Set(memoryReference, value!);
        }

        // Store the evaluated input value in the activity state.
        context.ActivityState[inputDescriptor.Name] = value!;
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

    /// <summary>
    /// Returns the outcome name for the specified port property name.
    /// </summary>
    /// <param name="context">The <see cref="ActivityExecutionContext"/> being extended.</param>
    /// <param name="portPropertyName">The name of the port property.</param>
    /// <returns>The outcome name.</returns>
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

    /// <summary>
    /// Evaluates the specified input and sets the result in the activity execution context's memory space.
    /// </summary>
    /// <param name="context">The <see cref="ActivityExecutionContext"/> being extended.</param>
    /// <param name="input">The input to evaluate.</param>
    /// <typeparam name="T">The type of the input.</typeparam>
    /// <returns>The evaluated value.</returns>
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
        context.WorkflowExecutionContext.ActiveActivityExecutionContexts.Where(x => x.ParentActivityExecutionContext == context);

    /// <summary>
    /// Removes all child <see cref="ActivityExecutionContext"/> objects.
    /// </summary>
    public static async Task RemoveChildrenAsync(this ActivityExecutionContext context)
    {
        // Detach child activity execution contexts.
        await context.WorkflowExecutionContext.RemoveActivityExecutionContextsAsync(context.GetChildren());
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
        // Mark the activity as complete.
        context.Status = ActivityStatus.Completed;
        
        // Add an execution log entry.
        context.AddExecutionLogEntry("Completed", payload: context.JournalData, includeActivityState: true);
        
        // Send a signal.
        await context.SendSignalAsync(new ActivityCompleted(result));

        // Remove the context.
        await context.WorkflowExecutionContext.CompleteActivityExecutionContextAsync(context);
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
        var publisher = context.GetRequiredService<INotificationSender>();
        context.Status = ActivityStatus.Canceled;
        context.ClearBookmarks();
        context.WorkflowExecutionContext.Bookmarks.RemoveWhere(x => x.ActivityNodeId == context.NodeId);
        
        // Add an execution log entry.
        context.AddExecutionLogEntry("Canceled", payload: context.JournalData, includeActivityState: true);
        
        await context.SendSignalAsync(new CancelSignal());
        await publisher.SendAsync(new ActivityCancelled(context));
    }
    
    /// <summary>
    /// Returns the first context that is associated with a <see cref="IVariableContainer"/>.
    /// </summary>
    public static ActivityExecutionContext? FindParentWithVariableContainer(this ActivityExecutionContext context)
    {
        return context.FindParent(x => x.Activity is IVariableContainer);
    }
    
    /// <summary>
    /// Returns the first context in the hierarchy that matches the specified predicate.
    /// </summary>
    /// <param name="context">The context to start searching from.</param>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>The first context that matches the predicate or <c>null</c> if no match was found.</returns>
    public static ActivityExecutionContext? FindParent(this ActivityExecutionContext context, Func<ActivityExecutionContext, bool> predicate)
    {
        var currentContext = context;

        while (currentContext != null)
        {
            if (predicate(currentContext))
                return currentContext;

            currentContext = currentContext.ParentActivityExecutionContext;
        }

        return null;
    }

    internal static bool GetHasEvaluatedProperties(this ActivityExecutionContext context) => context.TransientProperties.TryGetValue<bool>("HasEvaluatedProperties", out var value) && value;
    internal static void SetHasEvaluatedProperties(this ActivityExecutionContext context) => context.TransientProperties["HasEvaluatedProperties"] = true;
}