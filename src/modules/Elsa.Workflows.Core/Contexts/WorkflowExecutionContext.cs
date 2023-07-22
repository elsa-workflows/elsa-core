using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using Elsa.Common.Contracts;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core;

/// <summary>
/// A delegate entry that is used by activities to be notified when the activities they scheduled are completed.
/// </summary>
/// <param name="Owner">The activity scheduling the <see cref="Child"/> activity.</param>
/// <param name="Child">The child <see cref="IActivity"/> being scheduled.</param>
/// <param name="CompletionCallback">The <see cref="ActivityCompletionCallback"/> delegate to invoke when the scheduled <see cref="Child"/> activity completes.</param>
public record ActivityCompletionCallbackEntry(ActivityExecutionContext Owner, ActivityNode Child, ActivityCompletionCallback? CompletionCallback);

/// <summary>
/// Provides context to the currently executing workflow.
/// </summary>
[PublicAPI]
public class WorkflowExecutionContext : IExecutionContext
{
    private static readonly object ActivityOutputRegistryKey = new();
    private static readonly object LastActivityResultKey = new();
    internal static ValueTask Complete(ActivityExecutionContext context) => context.CompleteActivityAsync();
    private readonly IServiceProvider _serviceProvider;
    private readonly IHasher _hasher;
    private readonly ISystemClock _systemClock;
    private readonly IActivityRegistry _activityRegistry;
    private readonly IList<ActivityNode> _nodes;
    private readonly IList<ActivityCompletionCallbackEntry> _completionCallbackEntries = new List<ActivityCompletionCallbackEntry>();
    private IList<ActivityExecutionContext> _activityExecutionContexts;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowExecutionContext(
        IServiceProvider serviceProvider,
        IHasher hasher,
        ISystemClock systemClock,
        string id,
        string? correlationId,
        Workflow workflow,
        ActivityNode graph,
        ICollection<ActivityNode> nodes,
        IActivityScheduler scheduler,
        IActivityRegistry activityRegistry,
        IDictionary<string, object>? input,
        ExecuteActivityDelegate? executeDelegate,
        string? triggerActivityId,
        IEnumerable<ActivityExecutionContext>? activityExecutionContexts,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        _serviceProvider = serviceProvider;
        _hasher = hasher;
        _systemClock = systemClock;
        _activityRegistry = activityRegistry;
        Workflow = workflow;
        Graph = graph;
        SubStatus = WorkflowSubStatus.Executing;
        Id = id;
        CorrelationId = correlationId;
        _nodes = nodes.ToList();
        _activityExecutionContexts = activityExecutionContexts?.ToList() ?? new List<ActivityExecutionContext>();
        Scheduler = scheduler;
        Input = input != null ? new Dictionary<string, object>(input, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        ExecuteDelegate = executeDelegate;
        TriggerActivityId = triggerActivityId;
        CreatedAt = createdAt;
        CancellationToken = cancellationToken;
        NodeIdLookup = _nodes.ToDictionary(x => x.NodeId);
        NodeHashLookup = _nodes.ToDictionary(x => Hash(x.NodeId));
        NodeActivityLookup = _nodes.ToDictionary(x => x.Activity);
        MemoryRegister = workflow.CreateRegister();
        ExpressionExecutionContext = new ExpressionExecutionContext(serviceProvider, MemoryRegister, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// The <see cref="Workflow"/> associated with the execution context.
    /// </summary>
    public Workflow Workflow { get; }

    /// <summary>
    /// A graph of the workflow structure.
    /// </summary>
    public ActivityNode Graph { get; }

    /// <summary>
    /// The current status of the workflow. 
    /// </summary>
    public WorkflowStatus Status => GetMainStatus(SubStatus);

    /// <summary>
    /// The current sub status of the workflow.
    /// </summary>
    public WorkflowSubStatus SubStatus { get; internal set; }

    /// <summary>
    /// The root <see cref="MemoryRegister"/> associated with the execution context.
    /// </summary>
    public MemoryRegister MemoryRegister { get; }

    /// <summary>
    /// A unique ID of the execution context.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// An application-specific identifier associated with the execution context.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The date and time the workflow execution context was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// A flattened list of <see cref="ActivityNode"/>s from the <see cref="Graph"/>. 
    /// </summary>
    public IReadOnlyCollection<ActivityNode> Nodes => new ReadOnlyCollection<ActivityNode>(_nodes);

    /// <summary>
    /// A map between activity IDs and <see cref="ActivityNode"/>s in the workflow graph.
    /// </summary>
    public IDictionary<string, ActivityNode> NodeIdLookup { get; }

    /// <summary>
    /// A map between hashed activity node IDs and <see cref="ActivityNode"/>s in the workflow graph.
    /// </summary>
    public IDictionary<string, ActivityNode> NodeHashLookup { get; }

    /// <summary>
    /// A map between <see cref="IActivity"/>s and <see cref="ActivityNode"/>s in the workflow graph.
    /// </summary>
    public IDictionary<IActivity, ActivityNode> NodeActivityLookup { get; }

    /// <summary>
    /// The <see cref="IActivityScheduler"/> for the execution context.
    /// </summary>
    public IActivityScheduler Scheduler { get; }

    /// <summary>
    /// A collection of collected bookmarks during workflow execution. 
    /// </summary>
    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

    /// <summary>
    /// A dictionary of inputs provided at the start of the current workflow execution. 
    /// </summary>
    public IDictionary<string, object> Input { get; }

    /// <summary>
    /// A dictionary of outputs provided by the current workflow execution. 
    /// </summary>
    public IDictionary<string, object> Output { get; set; } = new Dictionary<string, object>();

    /// <inheritdoc />
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// A dictionary that can be used by application code and middleware to store information and even services. Values do not need to be serializable.
    /// All data will be gone once workflow execution completes. 
    /// </summary>
    public IDictionary<object, object> TransientProperties { get; set; } = new Dictionary<object, object>();

    /// <summary>
    /// Stores any fault that may have occurred during execution. Faulting a workflow will effectively suspend subsequent execution.
    /// </summary>
    public WorkflowFault? Fault { get; set; }

    /// <summary>
    /// The current <see cref="ExecuteActivityDelegate"/> delegate to invoke when executing the next activity.
    /// </summary>
    public ExecuteActivityDelegate? ExecuteDelegate { get; set; }

    /// <summary>
    /// Provides context about the bookmark that was used to resume workflow execution, if any. 
    /// </summary>
    public ResumedBookmarkContext? ResumedBookmarkContext { get; set; }

    /// <summary>
    /// The ID of the activity associated with the trigger that caused this workflow execution, if any. 
    /// </summary>
    public string? TriggerActivityId { get; set; }

    /// <summary>
    /// A <see cref="CancellationToken"/> that can be used to cancel asynchronous operations.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// A list of <see cref="ActivityCompletionCallbackEntry"/> callbacks that are invoked when the associated child activity completes.
    /// </summary>
    public ICollection<ActivityCompletionCallbackEntry> CompletionCallbacks => new ReadOnlyCollection<ActivityCompletionCallbackEntry>(_completionCallbackEntries);

    /// <summary>
    /// A list of <see cref="ActivityExecutionContext"/>s that are currently active.
    /// </summary>
    public IReadOnlyCollection<ActivityExecutionContext> ActiveActivityExecutionContexts => ActivityExecutionContexts.Where(x => !x.IsCompleted).ToList();

    /// <summary>
    /// A list of <see cref="ActivityExecutionContext"/>s that are currently active.
    /// </summary>
    public IReadOnlyCollection<ActivityExecutionContext> ActivityExecutionContexts
    {
        get => _activityExecutionContexts.ToList();
        internal set => _activityExecutionContexts = value.ToList();
    }

    /// <summary>
    /// The last execution log sequence number. This number is incremented every time a new entry is added to the execution log and is persisted alongside the workflow instance and restored when the workflow is resumed.
    /// </summary>
    public long ExecutionLogSequence { get; set; }

    /// <summary>
    /// A collection of execution log entries. This collection is flushed when the workflow execution context ends.
    /// </summary>
    public ICollection<WorkflowExecutionLogEntry> ExecutionLog { get; } = new List<WorkflowExecutionLogEntry>();

    /// <summary>
    /// The expression execution context for the current workflow execution.
    /// </summary>
    public ExpressionExecutionContext? ExpressionExecutionContext { get; } = default!;

    /// <inheritdoc />
    public IEnumerable<Variable> Variables => Workflow.Variables;

    /// <summary>
    /// Resolves the specified service type from the service provider.
    /// </summary>
    public T GetRequiredService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();

    /// <summary>
    /// Resolves the specified service type from the service provider.
    /// </summary>
    public object GetRequiredService(Type serviceType) => _serviceProvider.GetRequiredService(serviceType);

    /// <summary>
    /// Resolves the specified service type from the service provider, or creates a new instance if the service type was not found in the service container.
    /// </summary>
    public T GetOrCreateService<T>() where T : notnull => ActivatorUtilities.GetServiceOrCreateInstance<T>(_serviceProvider);

    /// <summary>
    /// Resolves the specified service type from the service provider, or creates a new instance if the service type was not found in the service container.
    /// </summary>
    public object GetOrCreateService(Type serviceType) => ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, serviceType);

    /// <summary>
    /// Resolves the specified service type from the service provider.
    /// </summary>
    public T? GetService<T>() where T : notnull => _serviceProvider.GetService<T>();

    /// <summary>
    /// Resolves the specified service type from the service provider.
    /// </summary>
    public object? GetService(Type serviceType) => _serviceProvider.GetService(serviceType);

    /// <summary>
    /// Resolves multiple implementations of the specified service type from the service provider.
    /// </summary>
    public IEnumerable<T> GetServices<T>() where T : notnull => _serviceProvider.GetServices<T>();

    /// <summary>
    /// Registers a completion callback for the specified activity.
    /// </summary>
    internal void AddCompletionCallback(ActivityExecutionContext owner, ActivityNode child, ActivityCompletionCallback? completionCallback = default)
    {
        var entry = new ActivityCompletionCallbackEntry(owner, child, completionCallback);
        _completionCallbackEntries.Add(entry);
    }

    /// <summary>
    /// Unregisters the completion callback for the specified owner and child activity.
    /// </summary>
    internal ActivityCompletionCallbackEntry? PopCompletionCallback(ActivityExecutionContext owner, ActivityNode child)
    {
        var entry = _completionCallbackEntries.FirstOrDefault(x => x.Owner == owner && x.Child == child);

        if (entry == null)
            return default;

        RemoveCompletionCallback(entry);
        return entry;
    }

    internal void RemoveCompletionCallback(ActivityCompletionCallbackEntry entry) => _completionCallbackEntries.Remove(entry);

    internal void RemoveCompletionCallbacks(IEnumerable<ActivityCompletionCallbackEntry> entries)
    {
        foreach (var entry in entries.ToList())
            _completionCallbackEntries.Remove(entry);
    }

    /// <summary>
    /// Returns the <see cref="ActivityNode"/> with the specified activity ID from the workflow graph.
    /// </summary>
    public ActivityNode FindNodeById(string nodeId) => NodeIdLookup[nodeId];

    /// <summary>
    /// Returns the <see cref="ActivityNode"/> with the specified hash of the activity node ID from the workflow graph.
    /// </summary>
    /// <param name="hash">The hash of the activity node ID.</param>
    /// <returns>The <see cref="ActivityNode"/> with the specified hash of the activity node ID.</returns>
    public ActivityNode FindNodeByHash(string hash) => NodeHashLookup[hash];

    /// <summary>
    /// Returns the <see cref="ActivityNode"/> containing the specified activity from the workflow graph.
    /// </summary>
    public ActivityNode FindNodeByActivity(IActivity activity) => NodeActivityLookup[activity];

    /// <summary>
    /// Returns the <see cref="IActivity"/> with the specified ID from the workflow graph.
    /// </summary>
    public IActivity FindActivityByNodeId(string nodeId) => FindNodeById(nodeId).Activity;

    /// <summary>
    /// Returns the <see cref="IActivity"/> with the specified ID from the workflow graph.
    /// </summary>
    public IActivity FindActivityByActivityId(string activityId) => FindNodeById(NodeIdLookup.Single(n => n.Key.Contains(activityId)).Value.NodeId).Activity;

    /// <summary>
    /// Returns the <see cref="IActivity"/> with the specified hash of the activity node ID from the workflow graph.
    /// </summary>
    /// <param name="hash">The hash of the activity node ID.</param>
    /// <returns>The <see cref="IActivity"/> with the specified hash of the activity node ID.</returns>
    public IActivity FindActivityByHash(string hash) => FindNodeByHash(hash).Activity;

    /// <summary>
    /// Returns a custom property with the specified key from the <see cref="Properties"/> dictionary.
    /// </summary>
    public T? GetProperty<T>(string key) => Properties.TryGetValue(key, out var value) ? value.ConvertTo<T>() : default;

    /// <summary>
    /// Sets a custom property with the specified key on the <see cref="Properties"/> dictionary.
    /// </summary>
    public void SetProperty<T>(string key, T value) => Properties[key] = value!;

    /// <summary>
    /// Updates a custom property with the specified key on the <see cref="Properties"/> dictionary.
    /// </summary>
    public T UpdateProperty<T>(string key, Func<T?, T> updater)
    {
        var value = GetProperty<T?>(key);
        value = updater(value);
        Properties[key] = value!;
        return value;
    }

    /// <summary>
    /// Returns true if the <see cref="Properties"/> dictionary contains the specified key.
    /// </summary>
    public bool HasProperty(string name) => Properties.ContainsKey(name);

    internal bool CanTransitionTo(WorkflowSubStatus targetSubStatus) => ValidateStatusTransition(targetSubStatus);

    internal void TransitionTo(WorkflowSubStatus subStatus)
    {
        if (!ValidateStatusTransition(SubStatus))
            throw new Exception($"Cannot transition from {SubStatus} to {subStatus}");

        SubStatus = subStatus;
    }

    /// <summary>
    /// Creates a new <see cref="ActivityExecutionContext"/> for the specified activity.
    /// </summary>
    public ActivityExecutionContext CreateActivityExecutionContext(IActivity activity, ActivityExecutionContext? parentContext = default)
    {
        var activityDescriptor = _activityRegistry.Find(activity) ?? throw new Exception($"Activity with type {activity.Type} not found in registry");
        var parentExpressionExecutionContext = parentContext?.ExpressionExecutionContext ?? ExpressionExecutionContext;
        var properties = ExpressionExecutionContextExtensions.CreateActivityExecutionContextPropertiesFrom(this, Input);
        var memory = new MemoryRegister();
        var now = _systemClock.UtcNow;
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, memory, parentExpressionExecutionContext, properties, CancellationToken);
        var activityExecutionContext = new ActivityExecutionContext(this, parentContext, expressionExecutionContext, activity, activityDescriptor, now, CancellationToken);
        expressionExecutionContext.TransientProperties[ExpressionExecutionContextExtensions.ActivityExecutionContextKey] = activityExecutionContext;
        return activityExecutionContext;
    }

    /// <summary>
    /// Returns a register of recorded activity output.
    /// </summary>
    public ActivityOutputRegister GetActivityOutputRegister() => TransientProperties.GetOrAdd(ActivityOutputRegistryKey, () => new ActivityOutputRegister());

    /// <summary>
    /// Returns the last activity result.
    /// </summary>
    public object? GetLastActivityResult() => TransientProperties.TryGetValue(LastActivityResultKey, out var value) ? value : default;

    /// <summary>
    /// Cancels the specified activity. 
    /// </summary>
    public async Task CancelActivityAsync(IActivity activity)
    {
        var activityExecutionContext = ActiveActivityExecutionContexts.FirstOrDefault(x => x.Activity == activity);

        if (activityExecutionContext != null)
            await activityExecutionContext.CancelActivityAsync();
    }

    /// <summary>
    /// Marks the specified activity execution context as completed.
    /// </summary>
    internal async Task CompleteActivityExecutionContextAsync(ActivityExecutionContext context)
    {
        // Select all child contexts.
        var childContexts = ActiveActivityExecutionContexts.Where(x => x.ParentActivityExecutionContext == context).ToList();

        foreach (var childContext in childContexts) await CompleteActivityExecutionContextAsync(childContext);

        // // Remove the context.
        //_activityExecutionContexts.Remove(context);
        context.CompletedAt = _systemClock.UtcNow;

        // Remove all associated completion callbacks.
        context.ClearCompletionCallbacks();

        // Remove all associated variables.
        var variablePersistenceManager = context.GetRequiredService<IVariablePersistenceManager>();
        await variablePersistenceManager.DeleteVariablesAsync(context);

        // Remove all associated bookmarks.
        Bookmarks.RemoveWhere(x => x.ActivityInstanceId == context.Id);
    }

    /// <summary>
    /// Adds the specified <see cref="ActivityExecutionContext"/> to the workflow execution context.
    /// </summary>
    internal void AddActivityExecutionContext(ActivityExecutionContext context) => _activityExecutionContexts.Add(context);

    /// <summary>
    /// Records the output of the specified activity into the current workflow execution context. 
    /// </summary>
    /// <param name="activityExecutionContext">The <see cref="ActivityExecutionContext"/> of the activity.</param>
    /// <param name="outputName">The name of the output.</param>
    /// <param name="value">The value of the output.</param>
    internal void RecordActivityOutput(ActivityExecutionContext activityExecutionContext, string? outputName, object? value)
    {
        var register = GetActivityOutputRegister();
        register.Record(activityExecutionContext, outputName, value);

        // If the output name is the default output name, record the value as the last activity result.
        if (outputName == ActivityOutputRegister.DefaultOutputName)
            TransientProperties[LastActivityResultKey] = value!;
    }

    private WorkflowStatus GetMainStatus(WorkflowSubStatus subStatus) =>
        subStatus switch
        {
            WorkflowSubStatus.Cancelled => WorkflowStatus.Finished,
            WorkflowSubStatus.Executing => WorkflowStatus.Running,
            WorkflowSubStatus.Faulted => WorkflowStatus.Finished,
            WorkflowSubStatus.Finished => WorkflowStatus.Finished,
            WorkflowSubStatus.Suspended => WorkflowStatus.Running,
            _ => throw new ArgumentOutOfRangeException(nameof(subStatus), subStatus, null)
        };

    private bool ValidateStatusTransition(WorkflowSubStatus targetSubStatus)
    {
        var currentMainStatus = GetMainStatus(SubStatus);
        return currentMainStatus != WorkflowStatus.Finished;
    }

    private string Hash(string nodeId) => _hasher.Hash(nodeId);
}