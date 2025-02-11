using System.Reflection;
using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
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
public static partial class ActivityExecutionContextExtensions
{
    /// <summary>
    /// Attempts to get a value from the input provided via <see cref="WorkflowExecutionContext"/>. If a value was found, an attempt is made to convert it into the specified type <code>T</code>.
    /// </summary>
    public static bool TryGetWorkflowInput<T>(this ActivityExecutionContext context, string key, out T value, JsonSerializerOptions? serializerOptions = null)
    {
        var wellKnownTypeRegistry = context.GetRequiredService<IWellKnownTypeRegistry>();

        if (context.WorkflowInput.TryGetValue(key, out var v))
        {
            value = v.ConvertTo<T>(new(serializerOptions, wellKnownTypeRegistry))!;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Gets a value from the input provided via <see cref="WorkflowExecutionContext"/>. If a value was found, an attempt is made to convert it into the specified type <code>T</code>.
    /// </summary>
    public static T GetWorkflowInput<T>(this ActivityExecutionContext context, JsonSerializerOptions? serializerOptions = null) => context.GetWorkflowInput<T>(typeof(T).Name, serializerOptions);

    /// <summary>
    /// Gets a value from the input provided via <see cref="WorkflowExecutionContext"/>. If a value was found, an attempt is made to convert it into the specified type <code>T</code>.
    /// </summary>
    public static T GetWorkflowInput<T>(this ActivityExecutionContext context, string key, JsonSerializerOptions? serializerOptions = null)
    {
        var wellKnownTypeRegistry = context.GetRequiredService<IWellKnownTypeRegistry>();
        return context.WorkflowInput[key].ConvertTo<T>(new(serializerOptions, wellKnownTypeRegistry))!;
    }

    /// <summary>
    /// Sets the Result property of the specified activity.
    /// </summary>
    /// <param name="context">The <see cref="ActivityExecutionContext"/></param> being extended.
    /// <param name="value">The value to set.</param>
    /// <exception cref="Exception">Thrown when the specified activity does not implement <see cref="IActivityWithResult"/>.</exception>
    public static void SetResult(this ActivityExecutionContext context, object? value)
    {
        var activity = context.Activity as IActivityWithResult ?? throw new($"Cannot set result on activity {context.Activity.Id} because it does not implement {nameof(IActivityWithResult)}.");
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
    public static Variable CreateVariable(this ActivityExecutionContext context, string name, object? value, Type? storageDriverType = null, Action<MemoryBlock>? configure = null) =>
        context.ExpressionExecutionContext.CreateVariable(name, value, storageDriverType, configure);

    /// <summary>
    /// Sets a workflow variable by name.
    /// </summary>
    /// <param name="context">The <see cref="ActivityExecutionContext"/> being extended.</param>
    /// <param name="name">The name of the variable.</param>
    /// <param name="value">The value of the variable.</param>
    /// <param name="configure">A callback to configure the memory block.</param>
    /// <returns>The created <see cref="Variable"/>.</returns>
    public static Variable SetVariable(this ActivityExecutionContext context, string name, object? value, Action<MemoryBlock>? configure = null) =>
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

            var serializedOutputValue = serializer.Serialize(outputValue);
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
    public static IEnumerable<ActivityExecutionContext> GetActiveChildren(this ActivityExecutionContext context)
    {
        return context.WorkflowExecutionContext.ActivityExecutionContexts.Where(x => x.ParentActivityExecutionContext == context);
    }

    /// <summary>
    /// Returns a flattened list of the current context's immediate children.
    /// </summary>
    public static IEnumerable<ActivityExecutionContext> GetChildren(this ActivityExecutionContext context)
    {
        return context.WorkflowExecutionContext.ActivityExecutionContexts.Where(x => x.ParentActivityExecutionContext == context);
    }

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

    internal static bool GetHasEvaluatedProperties(this ActivityExecutionContext context) => context.TransientProperties.TryGetValue<bool>("HasEvaluatedProperties", out var value) && value;
    internal static void SetHasEvaluatedProperties(this ActivityExecutionContext context) => context.TransientProperties["HasEvaluatedProperties"] = true;
}