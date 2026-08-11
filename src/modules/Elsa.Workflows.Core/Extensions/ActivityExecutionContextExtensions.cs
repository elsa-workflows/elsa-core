using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Elsa.Common;
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
using Elsa.Workflows.State;
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
    private const string ExtensionsMetadataKey = "Extensions";

    /// <param name="context">The <see cref="ActivityExecutionContext"/></param>
    extension(ActivityExecutionContext context)
    {
        /// <summary>
        /// Attempts to get a value from the input provided via <see cref="WorkflowExecutionContext"/>. If a value was found, an attempt is made to convert it into the specified type <code>T</code>.
        /// </summary>
        public bool TryGetWorkflowInput<T>(string key, out T value, JsonSerializerOptions? serializerOptions = null)
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
        public T GetWorkflowInput<T>(JsonSerializerOptions? serializerOptions = null) => context.GetWorkflowInput<T>(typeof(T).Name, serializerOptions);

        /// <summary>
        /// Gets a value from the input provided via <see cref="WorkflowExecutionContext"/>. If a value was found, an attempt is made to convert it into the specified type <code>T</code>.
        /// </summary>
        public T GetWorkflowInput<T>(string key, JsonSerializerOptions? serializerOptions = null)
        {
            var wellKnownTypeRegistry = context.GetRequiredService<IWellKnownTypeRegistry>();
            return context.WorkflowInput[key].ConvertTo<T>(new(serializerOptions, wellKnownTypeRegistry))!;
        }

        /// <summary>
        /// Sets the Result property of the specified activity.
        /// </summary>
        /// being extended.
        /// <param name="value">The value to set.</param>
        /// <exception cref="Exception">Thrown when the specified activity does not implement <see cref="IActivityWithResult"/>.</exception>
        public void SetResult(object? value)
        {
            var activity = context.Activity as IActivityWithResult ?? throw new($"Cannot set result on activity {context.Activity.Id} because it does not implement {nameof(IActivityWithResult)}.");
            context.Set(activity.Result, value, "Result");
        }

        /// <summary>
        /// Returns true if this activity is triggered for the first time and not being resumed.
        /// </summary>
        public bool IsTriggerOfWorkflow() => context.WorkflowExecutionContext.TriggerActivityId == context.Activity.Id;

        /// <summary>
        /// Creates a workflow variable by name and optionally sets the value.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The value of the variable.</param>
        /// <param name="storageDriverType">The type of storage driver to use for the variable.</param>
        /// <param name="configure">A callback to configure the memory block.</param>
        /// <returns>The created <see cref="Variable"/>.</returns>
        public Variable CreateVariable(string name, object? value, Type? storageDriverType = null, Action<MemoryBlock>? configure = null)
        {
            return context.ExpressionExecutionContext.CreateVariable(name, value, storageDriverType, configure);
        }

        /// <summary>
        /// Sets a workflow variable by name.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The value of the variable.</param>
        /// <param name="configure">A callback to configure the memory block.</param>
        /// <returns>The created <see cref="Variable"/>.</returns>
        public Variable SetVariable(string name, object? value, Action<MemoryBlock>? configure = null) =>
            context.ExpressionExecutionContext.SetVariable(name, value, configure);

        public Variable SetDynamicVariable<T>(string name, T value, Action<MemoryBlock>? configure = null)
        {
            // Check if a predefined variable already exists.
            var predefinedVariable = context.ExpressionExecutionContext.GetVariable(name);

            if (predefinedVariable != null)
            {
                context.SetVariable(name, value);
                return predefinedVariable;
            }

            // No predefined variable exists, so we will add a dynamic variable to the current container in scope.
            var container = context.FindParentWithVariableContainer();

            if (container == null)
                throw new("No parent variable container found");

            var existingVariable = container.DynamicVariables.FirstOrDefault(x => x.Name == name);

            if (existingVariable == null)
                container.DynamicVariables.Add(new Variable<T>(name, value));

            return context.SetVariable(name, value);
        }

        /// <summary>
        /// Gets a workflow variable by name.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <typeparam name="T">The type of the variable.</typeparam>
        /// <returns>The variable if found, otherwise null.</returns>
        public T? GetVariable<T>(string name) => context.ExpressionExecutionContext.GetVariable<T?>(name);

        /// <summary>
        /// Returns a dictionary of variable keys and their values across scopes.
        /// </summary>
        public IDictionary<string, object> GetVariableValues() => context.ExpressionExecutionContext.ReadAndFlattenMemoryBlocks();

        /// <summary>
        /// Returns a set of tuples containing the activity and its descriptor for all activities with outputs.
        /// </summary>
        public async IAsyncEnumerable<(IActivity Activity, ActivityDescriptor ActivityDescriptor)> GetActivitiesWithOutputs()
        {
            // Get current container.
            var currentContainerNode = context.FindParentWithVariableContainer()?.ActivityNode;

            if (currentContainerNode == null)
                yield break;

            // Get all nodes in the current container
            var workflowExecutionContext = context.WorkflowExecutionContext;
            var containedNodes = workflowExecutionContext.Nodes.Where(x => x.Parents.Contains(currentContainerNode)).Distinct().ToList();

            // Select activities with outputs.
            var activityRegistry = workflowExecutionContext.GetRequiredService<IActivityRegistryLookupService>();
            var activitiesWithOutputs = containedNodes.GetActivitiesWithOutputs(activityRegistry);

            await foreach (var (activity, activityDescriptor) in activitiesWithOutputs)
                yield return (activity, activityDescriptor);
        }
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

    /// <param name="activityExecutionContext">The <see cref="ActivityExecutionContext"/> being extended.</param>
    extension(ActivityExecutionContext activityExecutionContext)
    {
        /// <summary>
        /// Returns the activity with the specified ID or name.
        /// </summary>
        public IActivity? FindActivityByIdOrName(string idOrName)
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
        /// <param name="portPropertyName">The name of the port property.</param>
        /// <returns>The outcome name.</returns>
        public string GetOutcomeName(string portPropertyName)
        {
            var owner = activityExecutionContext.Activity;
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
        public async ValueTask SendSignalAsync(object signal)
        {
            await activityExecutionContext.TrySendSignalAsync(signal);
        }

        /// <summary>
        /// Send a signal up the current hierarchy of ancestors and report whether a receiver claimed it.
        /// </summary>
        /// <param name="signal">The signal to send.</param>
        /// <returns>
        /// <c>true</c> if a receiver called <see cref="SignalContext.StopPropagation"/>; otherwise, <c>false</c>.
        /// </returns>
        internal async ValueTask<bool> TrySendSignalAsync(object signal)
        {
            var receivingContexts = new[]
            {
                activityExecutionContext
            }.Concat(activityExecutionContext.GetAncestors()).ToList();
            var logger = activityExecutionContext.GetRequiredService<ILogger<ActivityExecutionContext>>();
            var signalType = signal.GetType();
            var signalTypeName = signalType.Name;

            // Let all ancestors receive the signal.
            foreach (var ancestorContext in receivingContexts)
            {
                var signalContext = new SignalContext(ancestorContext, activityExecutionContext, activityExecutionContext.CancellationToken);

                if (ancestorContext.Activity is not ISignalHandler handler)
                    continue;

                logger.LogDebug("Receiving signal {SignalType} on activity {ActivityId} of type {ActivityType}", signalTypeName, ancestorContext.Activity.Id, ancestorContext.Activity.Type);
                await handler.ReceiveSignalAsync(signal, signalContext);

                if (signalContext.StopPropagationRequested)
                {
                    logger.LogDebug("Propagation of signal {SignalType} on activity {ActivityId} of type {ActivityType} was stopped", signalTypeName, ancestorContext.Activity.Id, ancestorContext.Activity.Type);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Complete the current activity. This should only be called by activities that explicitly suppress automatic-completion.
        /// </summary>
        public async ValueTask ScheduleOutcomesAsync(params string[] outcomes)
        {
            // Record the outcomes, if any.
            activityExecutionContext.JournalData["Outcomes"] = outcomes;

            // Record the output, if any.
            var activity = activityExecutionContext.Activity;
            var expressionExecutionContext = activityExecutionContext.ExpressionExecutionContext;
            var activityDescriptor = activityExecutionContext.ActivityDescriptor;
            var outputDescriptors = activityDescriptor.Outputs;
            var outputs = outputDescriptors.ToDictionary(x => x.Name, x => activity.GetOutput(expressionExecutionContext, x.Name)!);
            var serializer = activityExecutionContext.GetRequiredService<ISafeSerializer>();

            foreach (var output in outputs)
            {
                var outputName = output.Key;
                var outputValue = output.Value;

                if (outputValue == null!)
                    continue;

                var serializedOutputValue = serializer.Serialize(outputValue);
                activityExecutionContext.JournalData[outputName] = serializedOutputValue;
            }

            // Send a signal.
            await activityExecutionContext.SendSignalAsync(new ScheduleActivityOutcomes(outcomes));
        }

        /// <summary>
        /// Cancel the activity. For blocking activities, it means their bookmarks will be removed. For job activities, the background work will be cancelled.
        /// </summary>
        public async Task CancelActivityAsync()
        {
            // If the activity is not running, do nothing.
            if (activityExecutionContext.Status != ActivityStatus.Running && activityExecutionContext.Status != ActivityStatus.Faulted)
                return;

            // Select all child contexts.
            var childContexts = activityExecutionContext.Children.ToList();

            foreach (var childContext in childContexts)
                await childContext.CancelActivityAsync();

            var publisher = activityExecutionContext.GetRequiredService<INotificationSender>();
            activityExecutionContext.TransitionTo(ActivityStatus.Canceled);
            activityExecutionContext.ClearBookmarks();
            activityExecutionContext.ClearCompletionCallbacks();
            activityExecutionContext.WorkflowExecutionContext.Bookmarks.RemoveWhere(x => x.ActivityNodeId == activityExecutionContext.NodeId);

            // Add an execution log entry.
            activityExecutionContext.AddExecutionLogEntry("Canceled");

            await activityExecutionContext.SendSignalAsync(new CancelSignal());
            await publisher.SendAsync(new ActivityCancelled(activityExecutionContext));
        }

        /// <summary>
        /// Marks the current activity as faulted, records an exception, and transitions its status to <see cref="ActivityStatus.Faulted"/>.
        /// An incident is logged, and the fault count for the activity and its ancestor activities is incremented.
        /// </summary>
        /// <param name="e">The exception to be recorded as the cause of the fault.</param>
        public void Fault(Exception e)
        {
            activityExecutionContext.Exception = e;
            activityExecutionContext.TransitionTo(ActivityStatus.Faulted);
            var activity = activityExecutionContext.Activity;
            var exceptionState = ExceptionState.FromException(e);
            var systemClock = activityExecutionContext.GetRequiredService<ISystemClock>();
            var now = systemClock.UtcNow;
            var incident = new ActivityIncident(activity.Id, activity.NodeId, activity.Type, e.Message, exceptionState, now);
            activityExecutionContext.WorkflowExecutionContext.Incidents.Add(incident);
            activityExecutionContext.AggregateFaultCount++;
        
            var ancestors = activityExecutionContext.GetAncestors();

            foreach (var ancestor in ancestors)
                ancestor.AggregateFaultCount++;
        }

        /// <summary>
        /// Recovers the current activity from a faulted state: undoes the fault counts, discards the incident and the recorded
        /// exception, and transitions the activity back to the running status.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the inverse of <c>Fault</c>, and is meant to leave no trace of a fault an enclosing container claimed. In
        /// particular it removes the <see cref="ActivityIncident"/> that <c>Fault</c> appended. A handled fault must not remain an
        /// incident, because plenty of code treats a non-empty <see cref="WorkflowExecutionContext.Incidents"/> as "this workflow
        /// failed" without looking further - the HTTP endpoint fault handler is one, and it would answer a caller with a fault
        /// response for a workflow that caught its error and completed normally. The execution log still records the failure, so
        /// nothing is hidden from anyone reading the journal.
        /// </para>
        /// <para>
        /// The incident is matched on this activity's node id, most recent first, which is the one <c>Fault</c> just appended.
        /// Pairing that way keeps an activity that faults, recovers, and faults again correct: each recovery removes its own
        /// incident rather than the whole activity's history.
        /// </para>
        /// <para>
        /// It remains asymmetric with <c>Fault</c> in one respect: it <i>sets</i> the current context's fault count to zero, which is
        /// idempotent, but <i>decrements</i> the count on every ancestor, which is not. Calling it more than once per fault drives
        /// ancestor counts negative. The status transition only applies to an activity that is still
        /// <see cref="ActivityStatus.Faulted"/>, so that a <see cref="Elsa.Workflows.Signals.FaultSignal"/> handler that already
        /// terminalized the activity does not see its decision undone.
        /// </para>
        /// </remarks>
        public void RecoverFromFault()
        {
            activityExecutionContext.AggregateFaultCount = 0;

            var ancestors = activityExecutionContext.GetAncestors();

            foreach (var ancestor in ancestors)
                ancestor.AggregateFaultCount--;

            var incidents = activityExecutionContext.WorkflowExecutionContext.Incidents;
            var ownIncident = incidents.LastOrDefault(x => x.ActivityNodeId == activityExecutionContext.NodeId);

            if (ownIncident != null)
                incidents.Remove(ownIncident);

            activityExecutionContext.Exception = null;

            if (activityExecutionContext.Status == ActivityStatus.Faulted)
                activityExecutionContext.TransitionTo(ActivityStatus.Running);
        }

        /// <summary>
        /// Returns the first context that is associated with a <see cref="IVariableContainer"/>.
        /// </summary>
        public ActivityExecutionContext? FindParentWithVariableContainer()
        {
            return activityExecutionContext.FindParent(x => x.Activity is IVariableContainer);
        }

        /// <summary>
        /// Returns the first context in the hierarchy that matches the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to match.</param>
        /// <returns>The first context that matches the predicate or <c>null</c> if no match was found.</returns>
        public ActivityExecutionContext? FindParent(Func<ActivityExecutionContext, bool> predicate)
        {
            var currentContext = activityExecutionContext;

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
        public IEnumerable<ActivityExecutionContext> GetAncestors()
        {
            var current = activityExecutionContext.ParentActivityExecutionContext;

            while (current != null)
            {
                yield return current;
                current = current.ParentActivityExecutionContext;
            }
        }

        /// <summary>
        /// Returns a flattened list of the current context's descendants.
        /// </summary>
        public IEnumerable<ActivityExecutionContext> GetDescendents()
        {
            var children = activityExecutionContext.Children.ToList();

            foreach (var child in children)
            {
                yield return child;

                foreach (var descendent in child.GetDescendents())
                    yield return descendent;
            }
        }

        /// <summary>
        /// Returns a flattened list of the current context's immediate active children.
        /// </summary>
        public IEnumerable<ActivityExecutionContext> GetActiveChildren()
        {
            return activityExecutionContext.Children;
        }

        /// <summary>
        /// Returns a flattened list of the current context's immediate children.
        /// </summary>
        public IEnumerable<ActivityExecutionContext> GetChildren()
        {
            return activityExecutionContext.Children;
        }

        /// <summary>
        /// Returns a flattened list of the current context's descendants.
        /// </summary>
        public IEnumerable<ActivityExecutionContext> GetDescendants()
        {
            var children = activityExecutionContext.Children.ToList();

            foreach (var child in children)
            {
                yield return child;

                foreach (var descendant in child.GetDescendants())
                    yield return descendant;
            }
        }

        /// <summary>
        /// Sets extension data in the metadata. Represents specific data that is exposed generically for an activity.
        /// </summary>
        public void SetExtensionsMetadata(string key, object? value)
        {
            var extensionsDictionary = activityExecutionContext.GetExtensionsMetadata() ?? new Dictionary<string, object?>();

            extensionsDictionary[key] = value;
        
            activityExecutionContext.Metadata[ExtensionsMetadataKey] = extensionsDictionary;
        }

        /// <summary>
        /// Retrieves the extension data from the metadata. Represents specific data that is exposed generically for an activity.
        /// </summary>
        public Dictionary<string, object?>? GetExtensionsMetadata()
        {
            return activityExecutionContext.Metadata.TryGetValue(ExtensionsMetadataKey, out var value) ? value as Dictionary<string, object?> : null;
        }

        /// <summary>
        /// Gets the output of an activity even when the context is not the same as the activity's context.
        /// This is useful when you want to get the output of a previously executed activity.
        /// </summary>
        /// <param name="property"></param>
        /// <typeparam name="TProp"></typeparam>
        /// <returns></returns>
        public object? GetActivityOutput<TProp>(Expression<Func<TProp>> property)
        {
            if (property.Body is not MemberExpression memberExpr)
            {
                return null;
            }
        
            var propertyName = memberExpr.Member.Name;
            var value = activityExecutionContext.Get(propertyName);
        
            if (value != null)
                return value;

            var registry = activityExecutionContext.WorkflowExecutionContext.GetActivityOutputRegister();
            
            return registry.FindOutputByActivityInstanceId(activityExecutionContext.Id, propertyName);
        }

        public bool GetHasEvaluatedProperties() => activityExecutionContext.TransientProperties.TryGetValue<bool>("HasEvaluatedProperties", out var value) && value;
        public void SetHasEvaluatedProperties() => activityExecutionContext.TransientProperties["HasEvaluatedProperties"] = true;

        /// <summary>
        /// Retrieves the complete execution chain for this activity execution context by traversing the SchedulingActivityExecutionId chain.
        /// Returns contexts ordered from root (depth 0) to this context.
        /// </summary>
        /// <returns>A collection of activity execution contexts representing the complete call stack, ordered from root to leaf.</returns>
        public IEnumerable<ActivityExecutionContext> GetExecutionChain()
        {
            var chain = new List<ActivityExecutionContext>();
            var visited = new HashSet<string>();
            var currentContext = activityExecutionContext;

            // Traverse the chain backwards from this context to the root
            while (currentContext != null && visited.Add(currentContext.Id))
            {
                chain.Add(currentContext);

                // Find the parent in the workflow execution context
                if (currentContext.SchedulingActivityExecutionId != null)
                {
                    currentContext = currentContext.WorkflowExecutionContext.ActivityExecutionContexts
                        .FirstOrDefault(x => x.Id == currentContext.SchedulingActivityExecutionId);
                }
                else
                {
                    break;
                }
            }

            // Reverse to get root-to-leaf order
            chain.Reverse();
            return chain;
        }
    }
}