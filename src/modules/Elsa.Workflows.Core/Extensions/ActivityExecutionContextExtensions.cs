using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Signals;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

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
    public static bool TryGetWorkflowInput<T>(this ActivityExecutionContext context, string key, out T value, JsonSerializerOptions? serializerOptions = default)
    {
        var wellKnownTypeRegistry = context.GetRequiredService<IWellKnownTypeRegistry>();

        if (context.WorkflowInput.TryGetValue(key, out var v))
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
    public static T GetWorkflowInput<T>(this ActivityExecutionContext context, JsonSerializerOptions? serializerOptions = default) => context.GetWorkflowInput<T>(typeof(T).Name, serializerOptions);

    /// <summary>
    /// Gets a value from the input provided via <see cref="WorkflowExecutionContext"/>. If a value was found, an attempt is made to convert it into the specified type <code>T</code>.
    /// </summary>
    public static T GetWorkflowInput<T>(this ActivityExecutionContext context, string key, JsonSerializerOptions? serializerOptions = default)
    {
        var wellKnownTypeRegistry = context.GetRequiredService<IWellKnownTypeRegistry>();
        return context.WorkflowInput[key].ConvertTo<T>(new ObjectConverterOptions(serializerOptions, wellKnownTypeRegistry))!;
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
        context.Set(activity.Result, value, "Result");
    }

    /// <summary>
    /// Returns true if this activity is triggered for the first time and not being resumed.
    /// </summary>
    public static bool IsTriggerOfWorkflow(this ActivityExecutionContext context) => context.WorkflowExecutionContext.TriggerActivityId == context.Activity.Id;

    /// <summary>
    /// Creates a workflow variable by name and optionally sets the value.
    /// </summary>
    /// <param name="context">The <see cref="ActivityExecutionContext"/> being extended.</param>
    /// <param name="name">The name of the variable.</param>
    /// <param name="value">The value of the variable.</param>
    /// <param name="storageDriverType">The type of storage driver to use for the variable.</param>
    /// <param name="configure">A callback to configure the memory block.</param>
    /// <returns>The created <see cref="Variable"/>.</returns>
    public static Variable CreateVariable(this ActivityExecutionContext context, string name, object? value, Type? storageDriverType = default, Action<MemoryBlock>? configure = default) =>
        context.ExpressionExecutionContext.CreateVariable(name, value, storageDriverType, configure);

    /// <summary>
    /// Sets a workflow variable by name.
    /// </summary>
    /// <param name="context">The <see cref="ActivityExecutionContext"/> being extended.</param>
    /// <param name="name">The name of the variable.</param>
    /// <param name="value">The value of the variable.</param>
    /// <param name="configure">A callback to configure the memory block.</param>
    /// <returns>The created <see cref="Variable"/>.</returns>
    public static Variable SetVariable(this ActivityExecutionContext context, string name, object? value, Action<MemoryBlock>? configure = default) =>
        context.ExpressionExecutionContext.SetVariable(name, value, configure);

    /// <summary>
    /// Gets a workflow variable by name.
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
        var activityDescriptor = context.ActivityDescriptor;
        var inputDescriptors = activityDescriptor.Inputs.Where(x => x.AutoEvaluate).ToList();

        // Evaluate inputs.
        foreach (var inputDescriptor in inputDescriptors)
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
        return input.ConvertTo<T>();
    }

    /// <summary>
    /// Evaluates a specific input property of the activity.
    /// </summary>
    public static async Task<object?> EvaluateInputPropertyAsync(this ActivityExecutionContext context, string inputName)
    {
        var activity = context.Activity;
        var activityRegistryLookup = context.GetRequiredService<IActivityRegistryLookupService>();
        var activityDescriptor = await activityRegistryLookup.FindAsync(activity.Type) ?? throw new Exception("Activity descriptor not found");
        var inputDescriptor = activityDescriptor.GetWrappedInputPropertyDescriptor(activity, inputName);

        if (inputDescriptor == null)
            throw new Exception($"No input with name {inputName} could be found");

        return await EvaluateInputPropertyAsync(context, activityDescriptor, inputDescriptor);
    }

    /// <summary>
    /// Returns a set of tuples containing the activity and its descriptor for all activities with outputs.
    /// </summary>
    /// <param name="activityExecutionContext">The <see cref="ActivityExecutionContext"/> being extended.</param>
    public static async IAsyncEnumerable<(IActivity Activity, ActivityDescriptor ActivityDescriptor)> GetActivitiesWithOutputs(this ActivityExecutionContext activityExecutionContext)
    {
        // Get current container.
        var currentContainerNode = activityExecutionContext.FindParentWithVariableContainer()?.ActivityNode;

        if (currentContainerNode == null)
            yield break;

        // Get all nodes in the current container
        var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
        var containedNodes = workflowExecutionContext.Nodes.Where(x => x.Parents.Contains(currentContainerNode)).Distinct().ToList();

        // Select activities with outputs.
        var activityRegistry = workflowExecutionContext.GetRequiredService<IActivityRegistryLookupService>();
        var activitiesWithOutputs = containedNodes.GetActivitiesWithOutputs(activityRegistry);

        await foreach (var (activity, activityDescriptor) in activitiesWithOutputs)
            yield return (activity, activityDescriptor);
    }

    /// <summary>
    /// Returns a set of tuples containing the activity and its descriptor for all activities with outputs.
    /// </summary>
    public static async IAsyncEnumerable<(IActivity activity, ActivityDescriptor activityDescriptor)> GetActivitiesWithOutputs(this IEnumerable<ActivityNode> nodes, IActivityRegistryLookupService activityRegistryLookup)
    {
        foreach (var node in nodes)
        {
            var activity = node.Activity;
            var activityDescriptor = await activityRegistryLookup.FindAsync(activity.Type, activity.Version);
            if (activityDescriptor != null && activityDescriptor.Outputs.Any()) 
                yield return (activity, activityDescriptor);
        }
    }

    /// <summary>
    /// Returns the activity with the specified ID or name.
    /// </summary>
    public static IActivity? FindActivityByIdOrName(this ActivityExecutionContext activityExecutionContext, string idOrName)
    {
        // Get current container.
        var currentContainerNode = activityExecutionContext.FindParentWithVariableContainer()?.ActivityNode;

        if (currentContainerNode == null)
            return null;

        // Get all nodes in the current container
        var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
        var containedNodes = workflowExecutionContext.Nodes.Where(x => x.Parents.Contains(currentContainerNode)).Distinct().ToList();
        var node = containedNodes.FirstOrDefault(x => x.Activity.Name == idOrName || x.Activity.Id == idOrName);
        return node?.Activity;
    }

    private static async Task<object?> EvaluateInputPropertyAsync(this ActivityExecutionContext context, ActivityDescriptor activityDescriptor, InputDescriptor inputDescriptor)
    {
        var activity = context.Activity;
        var defaultValue = inputDescriptor.DefaultValue;
        var value = defaultValue;
        var input = inputDescriptor.ValueGetter(activity);

        if (inputDescriptor.IsWrapped)
        {
            var wrappedInput = (Input?)input;

            if (defaultValue != null && wrappedInput == null)
            {
                var typedInput = typeof(Input<>).MakeGenericType(inputDescriptor.Type);
                var valueExpression = new Literal(defaultValue)
                {
                    Id = Guid.NewGuid().ToString()
                };
                wrappedInput = (Input)Activator.CreateInstance(typedInput, valueExpression)!;
                inputDescriptor.ValueSetter(activity, wrappedInput);
            }
            else
            {
                var evaluator = context.GetRequiredService<IExpressionEvaluator>();
                var expressionExecutionContext = context.ExpressionExecutionContext;
                value = wrappedInput?.Expression != null ? await evaluator.EvaluateAsync(wrappedInput, expressionExecutionContext) : defaultValue;
            }

            var memoryReference = wrappedInput?.MemoryBlockReference();

            // When input is created from an activity provider, there may be no memory block reference.
            if (memoryReference?.Id != null!)
            {
                // Declare the input memory block on the current context. 
                context.ExpressionExecutionContext.Set(memoryReference, value!);
            }
        }
        else
        {
            value = input;
        }

        // Store the serialized input value in the activity state.
        // Serializing the value ensures we store a copy of the value and not a reference to the input, which may change over time.
        if (inputDescriptor.IsSerializable != false)
        {
            var serializedValue = await context.GetRequiredService<ISafeSerializer>().SerializeToElementAsync(value);
            context.ActivityState[inputDescriptor.Name] = serializedValue;
        }

        return value;
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
    /// Returns a flattened list of the current context's descendants.
    /// </summary>
    public static IEnumerable<ActivityExecutionContext> GetDescendents(this ActivityExecutionContext context)
    {
        var children = context.WorkflowExecutionContext.ActivityExecutionContexts.Where(x => x.ParentActivityExecutionContext == context).ToList();

        foreach (var child in children)
        {
            yield return child;

            foreach (var descendent in GetDescendents(child))
                yield return descendent;
        }
    }

    /// <summary>
    /// Returns a flattened list of the current context's immediate active children.
    /// </summary>
    public static IEnumerable<ActivityExecutionContext> GetActiveChildren(this ActivityExecutionContext context) =>
        context.WorkflowExecutionContext.ActivityExecutionContexts.Where(x => x.ParentActivityExecutionContext == context);

    /// <summary>
    /// Returns a flattened list of the current context's immediate children.
    /// </summary>
    public static IEnumerable<ActivityExecutionContext> GetChildren(this ActivityExecutionContext context) =>
        context.WorkflowExecutionContext.ActivityExecutionContexts.Where(x => x.ParentActivityExecutionContext == context);

    /// <summary>
    /// Returns a flattened list of the current context's descendants.
    /// </summary>
    public static IEnumerable<ActivityExecutionContext> GetDescendants(this ActivityExecutionContext context)
    {
        var children = context.WorkflowExecutionContext.ActivityExecutionContexts.Where(x => x.ParentActivityExecutionContext == context).ToList();

        foreach (var child in children)
        {
            yield return child;

            foreach (var descendant in child.GetDescendants())
                yield return descendant;
        }
    }

    /// <summary>
    /// Send a signal up the current hierarchy of ancestors.
    /// </summary>
    public static async ValueTask SendSignalAsync(this ActivityExecutionContext context, object signal)
    {
        var receivingContexts = new[] { context }.Concat(context.GetAncestors()).ToList();
        var logger = context.GetRequiredService<ILogger<ActivityExecutionContext>>();
        var signalType = signal.GetType();
        var signalTypeName = signalType.Name;

        // Let all ancestors receive the signal.
        foreach (var ancestorContext in receivingContexts)
        {
            var signalContext = new SignalContext(ancestorContext, context, context.CancellationToken);

            if (ancestorContext.Activity is not ISignalHandler handler)
                continue;

            logger.LogDebug("Receiving signal {SignalType} on activity {ActivityId} of type {ActivityType}", signalTypeName, ancestorContext.Activity.Id, ancestorContext.Activity.Type);
            await handler.ReceiveSignalAsync(signal, signalContext);

            if (signalContext.StopPropagationRequested)
            {
                logger.LogDebug("Propagation of signal {SignalType} on activity {ActivityId} of type {ActivityType} was stopped", signalTypeName, ancestorContext.Activity.Id, ancestorContext.Activity.Type);
                return;
            }
        }
    }

    /// <summary>
    /// Complete the current activity. This should only be called by activities that explicitly suppress automatic-completion.
    /// </summary>
    public static async ValueTask ScheduleOutcomesAsync(this ActivityExecutionContext context, params string[] outcomes)
    {
        var cancellationToken = context.CancellationToken;

        // Record the outcomes, if any.
        context.JournalData["Outcomes"] = outcomes;

        // Record the output, if any.
        var activity = context.Activity;
        var expressionExecutionContext = context.ExpressionExecutionContext;
        var activityDescriptor = context.ActivityDescriptor;
        var outputDescriptors = activityDescriptor.Outputs;
        var outputs = outputDescriptors.ToDictionary(x => x.Name, x => activity.GetOutput(expressionExecutionContext, x.Name)!);
        var serializer = context.GetRequiredService<ISafeSerializer>();

        foreach (var output in outputs)
        {
            var outputName = output.Key;
            var outputValue = output.Value;

            if (outputValue == null!)
                continue;

            var serializedOutputValue = await serializer.SerializeAsync(outputValue, cancellationToken);
            context.JournalData[outputName] = serializedOutputValue;
        }

        // Send a signal.
        await context.SendSignalAsync(new ScheduleActivityOutcomes(outcomes));
    }
    
    /// <summary>
    /// Cancel the activity. For blocking activities, it means their bookmarks will be removed. For job activities, the background work will be cancelled.
    /// </summary>
    public static async Task CancelActivityAsync(this ActivityExecutionContext context)
    {
        // If the activity is not running, do nothing.
        if (context.Status != ActivityStatus.Running && context.Status != ActivityStatus.Faulted)
            return;

        // Select all child contexts.
        var childContexts = context.WorkflowExecutionContext.ActivityExecutionContexts.Where(x => x.ParentActivityExecutionContext == context).ToList();

        foreach (var childContext in childContexts)
            await CancelActivityAsync(childContext);

        var publisher = context.GetRequiredService<INotificationSender>();
        context.TransitionTo(ActivityStatus.Canceled);
        context.ClearBookmarks();
        context.ClearCompletionCallbacks();
        context.WorkflowExecutionContext.Bookmarks.RemoveWhere(x => x.ActivityNodeId == context.NodeId);

        // Add an execution log entry.
        context.AddExecutionLogEntry("Canceled", payload: context.JournalData);

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