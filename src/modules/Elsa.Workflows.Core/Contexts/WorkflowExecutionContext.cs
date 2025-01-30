using System.Collections.ObjectModel;
using Elsa.Common;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.State;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows;

/// <summary>
/// A delegate entry that is used by activities to be notified when the activities they scheduled are completed.
/// </summary>
/// <param name="Owner">The activity scheduling the <see cref="Child"/> activity.</param>
/// <param name="Child">The child <see cref="IActivity"/> being scheduled.</param>
/// <param name="CompletionCallback">The <see cref="ActivityCompletionCallback"/> delegate to invoke when the scheduled <see cref="Child"/> activity completes.</param>
/// <param name="Tag">An optional tag.</param>
public record ActivityCompletionCallbackEntry(ActivityExecutionContext Owner, ActivityNode Child, ActivityCompletionCallback? CompletionCallback, object? Tag = null);

/// <summary>
/// Provides context to the currently executing workflow.
/// </summary>
[PublicAPI]
public partial class WorkflowExecutionContext : IExecutionContext
{
    private static readonly object ActivityOutputRegistryKey = new();
    private static readonly object LastActivityResultKey = new();
    internal static ValueTask Complete(ActivityExecutionContext context) => context.CompleteActivityAsync();
    internal static ValueTask Noop(ActivityExecutionContext context) => default;
    private readonly IList<ActivityCompletionCallbackEntry> _completionCallbackEntries = new List<ActivityCompletionCallbackEntry>();
    private IList<ActivityExecutionContext> _activityExecutionContexts;
    private readonly IHasher _hasher;
    private readonly ICommitStateHandler _commitStateHandler;

    /// <summary>
    /// Initializes a new instance of <see cref="WorkflowExecutionContext"/>.
    /// </summary>
    private WorkflowExecutionContext(
        IServiceProvider serviceProvider,
        WorkflowGraph workflowGraph,
        string id,
        string? correlationId,
        string? parentWorkflowInstanceId,
        IDictionary<string, object>? input,
        IDictionary<string, object>? properties,
        ExecuteActivityDelegate? executeDelegate,
        string? triggerActivityId,
        IEnumerable<ActivityIncident> incidents,
        IEnumerable<Bookmark> originalBookmarks,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        ServiceProvider = serviceProvider;
        SystemClock = serviceProvider.GetRequiredService<ISystemClock>();
        ActivityRegistry = serviceProvider.GetRequiredService<IActivityRegistry>();
        ActivityRegistryLookup = serviceProvider.GetRequiredService<IActivityRegistryLookupService>();
        _hasher = serviceProvider.GetRequiredService<IHasher>();
        _commitStateHandler = serviceProvider.GetRequiredService<ICommitStateHandler>();
        SubStatus = WorkflowSubStatus.Pending;
        Id = id;
        CorrelationId = correlationId;
        ParentWorkflowInstanceId = parentWorkflowInstanceId;
        _activityExecutionContexts = new List<ActivityExecutionContext>();
        Scheduler = serviceProvider.GetRequiredService<IActivitySchedulerFactory>().CreateScheduler();
        IdentityGenerator = serviceProvider.GetRequiredService<IIdentityGenerator>();
        Input = input != null ? new Dictionary<string, object>(input, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        Properties = properties != null ? new Dictionary<string, object>(properties, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        ExecuteDelegate = executeDelegate;
        TriggerActivityId = triggerActivityId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        CancellationToken = cancellationToken;
        Incidents = incidents.ToList();
        OriginalBookmarks = originalBookmarks.ToList();
        WorkflowGraph = workflowGraph;
        var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancellationTokenSources.Add(linkedCancellationTokenSource);
        _cancellationRegistrations.Add(linkedCancellationTokenSource.Token.Register(CancelWorkflow));
    }

    /// <summary>
    /// Creates a new <see cref="WorkflowExecutionContext"/> for the specified workflow.
    /// </summary>
    public static async Task<WorkflowExecutionContext> CreateAsync(
        IServiceProvider serviceProvider,
        WorkflowGraph workflowGraph,
        string id,
        string? correlationId = null,
        string? parentWorkflowInstanceId = null,
        IDictionary<string, object>? input = null,
        IDictionary<string, object>? properties = null,
        ExecuteActivityDelegate? executeDelegate = null,
        string? triggerActivityId = null,
        CancellationToken cancellationToken = default)
    {
        var systemClock = serviceProvider.GetRequiredService<ISystemClock>();

        return await CreateAsync(
            serviceProvider,
            workflowGraph,
            id,
            new List<ActivityIncident>(),
            new List<Bookmark>(),
            systemClock.UtcNow,
            correlationId,
            parentWorkflowInstanceId,
            input,
            properties,
            executeDelegate,
            triggerActivityId,
            cancellationToken
        );
    }

    /// <summary>
    /// Creates a new <see cref="WorkflowExecutionContext"/> for the specified workflow.
    /// </summary>
    public static async Task<WorkflowExecutionContext> CreateAsync(
        IServiceProvider serviceProvider,
        WorkflowGraph workflowGraph,
        WorkflowState workflowState,
        string? correlationId = null,
        string? parentWorkflowInstanceId = null,
        IDictionary<string, object>? input = null,
        IDictionary<string, object>? properties = null,
        ExecuteActivityDelegate? executeDelegate = null,
        string? triggerActivityId = null,
        CancellationToken cancellationToken = default)
    {
        var workflowExecutionContext = await CreateAsync(
            serviceProvider,
            workflowGraph,
            workflowState.Id,
            workflowState.Incidents,
            workflowState.Bookmarks,
            workflowState.CreatedAt,
            correlationId,
            parentWorkflowInstanceId,
            input,
            properties,
            executeDelegate,
            triggerActivityId,
            cancellationToken);

        var workflowStateExtractor = serviceProvider.GetRequiredService<IWorkflowStateExtractor>();
        await workflowStateExtractor.ApplyAsync(workflowExecutionContext, workflowState);

        return workflowExecutionContext;
    }

    /// <summary>
    /// Creates a new <see cref="WorkflowExecutionContext"/> for the specified workflow.
    /// </summary>
    public static async Task<WorkflowExecutionContext> CreateAsync(
        IServiceProvider serviceProvider,
        WorkflowGraph workflowGraph,
        string id,
        IEnumerable<ActivityIncident> incidents,
        IEnumerable<Bookmark> originalBookmarks,
        DateTimeOffset createdAt,
        string? correlationId = null,
        string? parentWorkflowInstanceId = null,
        IDictionary<string, object>? input = null,
        IDictionary<string, object>? properties = null,
        ExecuteActivityDelegate? executeDelegate = null,
        string? triggerActivityId = null,
        CancellationToken cancellationToken = default)
    {
        // Set up a workflow execution context.
        var workflowExecutionContext = new WorkflowExecutionContext(
            serviceProvider,
            workflowGraph,
            id,
            correlationId,
            parentWorkflowInstanceId,
            input,
            properties,
            executeDelegate,
            triggerActivityId,
            incidents,
            originalBookmarks,
            createdAt,
            cancellationToken)
        {
            MemoryRegister = workflowGraph.Workflow.CreateRegister()
        };

        workflowExecutionContext.ExpressionExecutionContext = new ExpressionExecutionContext(serviceProvider, workflowExecutionContext.MemoryRegister, cancellationToken: cancellationToken);

        await workflowExecutionContext.SetWorkflowGraphAsync(workflowGraph);
        return workflowExecutionContext;
    }

    /// <summary>
    /// Assigns the specified workflow to this workflow execution context.
    /// </summary>
    /// <param name="workflowGraph">The workflow graph to assign.</param>
    public async Task SetWorkflowGraphAsync(WorkflowGraph workflowGraph)
    {
        WorkflowGraph = workflowGraph;
        var nodes = workflowGraph.Nodes;

        // Register activity types.
        var activityTypes = nodes.Select(x => x.Activity.GetType()).Distinct().ToList();
        await ActivityRegistry.RegisterAsync(activityTypes, CancellationToken);

        // Update the activity execution contexts with the actual activity instances.
        foreach (var activityExecutionContext in ActivityExecutionContexts)
            activityExecutionContext.Activity = workflowGraph.NodeIdLookup[activityExecutionContext.Activity.NodeId].Activity;
    }

    /// Gets the <see cref="IServiceProvider"/>.
    public IServiceProvider ServiceProvider { get; }

    /// Gets the <see cref="IActivityRegistry"/>.
    public IActivityRegistry ActivityRegistry { get; }

    /// Gets the <see cref="IActivityRegistryLookupService"/>.
    public IActivityRegistryLookupService ActivityRegistryLookup { get; }

    /// Gets the workflow graph.
    public WorkflowGraph WorkflowGraph { get; private set; }

    /// The <see cref="Workflow"/> associated with the execution context.
    public Workflow Workflow => WorkflowGraph.Workflow;

    /// A graph of the workflow structure.
    public ActivityNode Graph => WorkflowGraph.Root;

    /// The current status of the workflow.
    public WorkflowStatus Status => GetMainStatus(SubStatus);

    /// The current sub status of the workflow.
    public WorkflowSubStatus SubStatus { get; internal set; }

    /// The root <see cref="MemoryRegister"/> associated with the execution context.
    public MemoryRegister MemoryRegister { get; private set; } = null!;

    /// A unique ID of the execution context.
    public string Id { get; set; }

    /// <inheritdoc />
    public IActivity Activity => Workflow;

    /// An application-specific identifier associated with the execution context.
    public string? CorrelationId { get; set; }

    /// The ID of the workflow instance that triggered this instance.
    public string? ParentWorkflowInstanceId { get; set; }

    /// The date and time the workflow execution context was created.
    public DateTimeOffset CreatedAt { get; set; }

    /// The date and time the workflow execution context was last updated.
    public DateTimeOffset UpdatedAt { get; set; }

    /// The date and time the workflow execution context has finished.
    public DateTimeOffset? FinishedAt { get; set; }

    /// Gets the clock used to determine the current time.
    public ISystemClock SystemClock { get; }

    /// A flattened list of <see cref="ActivityNode"/>s from the <see cref="Graph"/>. 
    public IReadOnlyCollection<ActivityNode> Nodes => WorkflowGraph.Nodes.ToList();

    /// A map between activity IDs and <see cref="ActivityNode"/>s in the workflow graph.
    public IDictionary<string, ActivityNode> NodeIdLookup => WorkflowGraph.NodeIdLookup;

    /// A map between hashed activity node IDs and <see cref="ActivityNode"/>s in the workflow graph.
    public IDictionary<string, ActivityNode> NodeHashLookup => WorkflowGraph.NodeHashLookup;

    /// A map between <see cref="IActivity"/>s and <see cref="ActivityNode"/>s in the workflow graph.
    public IDictionary<IActivity, ActivityNode> NodeActivityLookup => WorkflowGraph.NodeActivityLookup;

    /// The <see cref="IActivityScheduler"/> for the execution context.
    public IActivityScheduler Scheduler { get; }

    /// Gets the <see cref="IIdentityGenerator"/>.
    public IIdentityGenerator IdentityGenerator { get; }

    /// Gets the collection of original bookmarks associated with the workflow execution context.
    public ICollection<Bookmark> OriginalBookmarks { get; set; }

    /// A collection of collected bookmarks during workflow execution. 
    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

    /// A diff between the original bookmarks and the current bookmarks.
    public Diff<Bookmark> BookmarksDiff => Diff.For(OriginalBookmarks, Bookmarks);

    /// <summary>
    /// A dictionary of inputs provided at the start of the current workflow execution.
    /// </summary>
    public IDictionary<string, object> Input { get; set; }

    /// <summary>
    /// A dictionary of outputs provided by the current workflow execution.
    /// </summary>
    public IDictionary<string, object> Output { get; set; } = new Dictionary<string, object>();

    /// <inheritdoc />
    public IDictionary<string, object> Properties { get; set; }

    /// <summary>
    /// A dictionary that can be used by application code and middleware to store information and even services. Values do not need to be serializable.
    /// All data will be gone once workflow execution completes.
    /// </summary>
    public IDictionary<object, object> TransientProperties { get; set; } = new Dictionary<object, object>();

    /// <summary>
    /// A collection of incidents that may have occurred during execution.
    /// </summary>
    public ICollection<ActivityIncident> Incidents { get; set; }

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
    /// A set of cancellation tokens that can be used to cancel the workflow execution without cancelling system-level operations.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// A list of <see cref="ActivityCompletionCallbackEntry"/> callbacks that are invoked when the associated child activity completes.
    /// </summary>
    public ICollection<ActivityCompletionCallbackEntry> CompletionCallbacks => new ReadOnlyCollection<ActivityCompletionCallbackEntry>(_completionCallbackEntries);

    /// <summary>
    /// A list of <see cref="ActivityExecutionContext"/>s that are currently active.
    /// </summary>
    public IReadOnlyCollection<ActivityExecutionContext> ActivityExecutionContexts
    {
        get => _activityExecutionContexts.AsReadOnly();
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
    public ExpressionExecutionContext? ExpressionExecutionContext { get; private set; }

    /// <inheritdoc />
    public IEnumerable<Variable> Variables => Workflow.Variables;

    /// <summary>
    /// Resolves the specified service type from the service provider.
    /// </summary>
    public T GetRequiredService<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();

    /// <summary>
    /// Resolves the specified service type from the service provider.
    /// </summary>
    public object GetRequiredService(Type serviceType) => ServiceProvider.GetRequiredService(serviceType);

    /// <summary>
    /// Resolves the specified service type from the service provider, or creates a new instance if the service type was not found in the service container.
    /// </summary>
    public T GetOrCreateService<T>() where T : notnull => ActivatorUtilities.GetServiceOrCreateInstance<T>(ServiceProvider);

    /// <summary>
    /// Resolves the specified service type from the service provider, or creates a new instance if the service type was not found in the service container.
    /// </summary>
    public object GetOrCreateService(Type serviceType) => ActivatorUtilities.GetServiceOrCreateInstance(ServiceProvider, serviceType);

    /// <summary>
    /// Resolves the specified service type from the service provider.
    /// </summary>
    public T? GetService<T>() where T : notnull => ServiceProvider.GetService<T>();

    /// <summary>
    /// Resolves the specified service type from the service provider.
    /// </summary>
    public object? GetService(Type serviceType) => ServiceProvider.GetService(serviceType);

    /// <summary>
    /// Resolves multiple implementations of the specified service type from the service provider.
    /// </summary>
    public IEnumerable<T> GetServices<T>() where T : notnull => ServiceProvider.GetServices<T>();

    /// <summary>
    /// Registers a completion callback for the specified activity.
    /// </summary>
    internal void AddCompletionCallback(ActivityExecutionContext owner, ActivityNode child, ActivityCompletionCallback? completionCallback = null, object? tag = null)
    {
        var entry = new ActivityCompletionCallbackEntry(owner, child, completionCallback, tag);
        _completionCallbackEntries.Add(entry);
    }

    /// <summary>
    /// Unregisters the completion callback for the specified owner and child activity.
    /// </summary>
    internal ActivityCompletionCallbackEntry? PopCompletionCallback(ActivityExecutionContext owner, ActivityNode child)
    {
        var entry = _completionCallbackEntries.FirstOrDefault(x => x.Owner == owner && x.Child == child);

        if (entry == null)
            return null;

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
    /// Finds the activity based on the provided <paramref name="handle"/>.
    /// </summary>
    /// <param name="handle">The handle containing the identification parameters for the activity.</param>
    /// <returns>The activity found based on the handle, or null if no activity is found.</returns>
    public IActivity? FindActivity(ActivityHandle handle)
    {
        return handle.ActivityId != null
            ? FindActivityById(handle.ActivityId)
            : handle.ActivityNodeId != null
                ? FindActivityByNodeId(handle.ActivityNodeId)
                : handle.ActivityInstanceId != null
                    ? FindActivityByInstanceId(handle.ActivityInstanceId)
                    : handle.ActivityHash != null
                        ? FindActivityByHash(handle.ActivityHash)
                        : null;
    }

    /// <summary>
    /// Returns the <see cref="ActivityNode"/> with the specified activity ID from the workflow graph.
    /// </summary>
    public ActivityNode? FindNodeById(string nodeId) => NodeIdLookup.TryGetValue(nodeId, out var node) ? node : null;

    /// <summary>
    /// Returns the <see cref="ActivityNode"/> with the specified hash of the activity node ID from the workflow graph.
    /// </summary>
    /// <param name="hash">The hash of the activity node ID.</param>
    /// <returns>The <see cref="ActivityNode"/> with the specified hash of the activity node ID.</returns>
    public ActivityNode? FindNodeByHash(string hash) => NodeHashLookup.TryGetValue(hash, out var node) ? node : null;

    /// Returns the <see cref="ActivityNode"/> containing the specified activity from the workflow graph.
    public ActivityNode? FindNodeByActivity(IActivity activity)
    {
        return NodeActivityLookup.TryGetValue(activity, out var node) ? node : null;
    }

    /// Returns the <see cref="ActivityNode"/> associated with the specified activity ID.
    public ActivityNode? FindNodeByActivityId(string activityId) => Nodes.FirstOrDefault(x => x.Activity.Id == activityId);

    /// Returns the <see cref="IActivity"/> with the specified ID from the workflow graph.
    public IActivity? FindActivityByNodeId(string nodeId) => FindNodeById(nodeId)?.Activity;

    /// Returns the <see cref="IActivity"/> with the specified ID from the workflow graph.
    public IActivity? FindActivityById(string activityId) => FindNodeById(NodeIdLookup.SingleOrDefault(n => n.Key.EndsWith(activityId)).Value.NodeId)?.Activity;

    /// Returns the <see cref="IActivity"/> with the specified hash of the activity node ID from the workflow graph.
    /// <param name="hash">The hash of the activity node ID.</param>
    /// <returns>The <see cref="IActivity"/> with the specified hash of the activity node ID.</returns>
    public IActivity? FindActivityByHash(string hash) => FindNodeByHash(hash)?.Activity;

    /// Returns the <see cref="ActivityExecutionContext"/> with the specified activity instance ID.
    public IActivity? FindActivityByInstanceId(string activityInstanceId) => ActivityExecutionContexts.FirstOrDefault(x => x.Id == activityInstanceId)?.Activity;

    /// Returns a custom property with the specified key from the <see cref="Properties"/> dictionary.
    public T? GetProperty<T>(string key) => Properties.TryGetValue(key, out var value) ? value.ConvertTo<T>() : default;

    /// Sets a custom property with the specified key on the <see cref="Properties"/> dictionary.
    public void SetProperty<T>(string key, T value) => Properties[key] = value!;

    /// Updates a custom property with the specified key on the <see cref="Properties"/> dictionary.
    public T UpdateProperty<T>(string key, Func<T?, T> updater)
    {
        var value = GetProperty<T?>(key);
        value = updater(value);
        Properties[key] = value!;
        return value;
    }

    /// Returns true if the <see cref="Properties"/> dictionary contains the specified key.
    public bool HasProperty(string name) => Properties.ContainsKey(name);

    internal bool CanTransitionTo(WorkflowSubStatus targetSubStatus) => ValidateStatusTransition();

    internal void TransitionTo(WorkflowSubStatus subStatus)
    {
        if (!ValidateStatusTransition())
            throw new($"Cannot transition from {SubStatus} to {subStatus}");

        SubStatus = subStatus;
        UpdatedAt = SystemClock.UtcNow;

        if (Status == WorkflowStatus.Finished)
            FinishedAt = UpdatedAt;

        if (Status == WorkflowStatus.Finished || SubStatus == WorkflowSubStatus.Suspended)
        {
            foreach (var registration in _cancellationRegistrations)
                registration.Dispose();
        }
    }

    /// Creates a new <see cref="ActivityExecutionContext"/> for the specified activity.
    public async Task<ActivityExecutionContext> CreateActivityExecutionContextAsync(IActivity activity, ActivityInvocationOptions? options = null)
    {
        var activityDescriptor = await ActivityRegistryLookup.FindAsync(activity) ?? throw new ActivityNotFoundException(activity.Type);
        var tag = options?.Tag;
        var parentContext = options?.Owner;
        var now = SystemClock.UtcNow;
        var id = IdentityGenerator.GenerateId();
        var activityExecutionContext = new ActivityExecutionContext(id, this, parentContext, activity, activityDescriptor, now, tag, SystemClock, CancellationToken);
        var variablesToDeclare = options?.Variables ?? Array.Empty<Variable>();
        var variableContainer = new[]
        {
            activityExecutionContext.ActivityNode
        }.Concat(activityExecutionContext.ActivityNode.Ancestors()).FirstOrDefault(x => x.Activity is IVariableContainer)?.Activity as IVariableContainer;
        activityExecutionContext.ExpressionExecutionContext.TransientProperties[ExpressionExecutionContextExtensions.ActivityExecutionContextKey] = activityExecutionContext;

        if (variableContainer != null)
        {
            foreach (var variable in variablesToDeclare)
            {
                // Declare a dynamic variable on the activity execution context.
                activityExecutionContext.DynamicVariables.RemoveWhere(x => x.Name == variable.Name);
                activityExecutionContext.DynamicVariables.Add(variable);

                // Assign the variable to the expression execution context.
                activityExecutionContext.ExpressionExecutionContext.CreateVariable(variable.Name, variable.Value);
            }
        }

        var activityInput = options?.Input ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        activityExecutionContext.ActivityInput.Merge(activityInput);

        return activityExecutionContext;
    }

    /// Returns a register of recorded activity output.
    public ActivityOutputRegister GetActivityOutputRegister() => TransientProperties.GetOrAdd(ActivityOutputRegistryKey, () => new ActivityOutputRegister());

    /// Returns the last activity result.
    public object? GetLastActivityResult() => TransientProperties.TryGetValue(LastActivityResultKey, out var value) ? value : null;

    /// Adds the specified <see cref="ActivityExecutionContext"/> to the workflow execution context.
    public void AddActivityExecutionContext(ActivityExecutionContext context) => _activityExecutionContexts.Add(context);

    /// Removes the specified <see cref="ActivityExecutionContext"/> from the workflow execution context.
    public void RemoveActivityExecutionContext(ActivityExecutionContext context)
    {
        _activityExecutionContexts.Remove(context);
        context.ParentActivityExecutionContext?.Children.Remove(context);
    }

    /// Removes the specified <see cref="ActivityExecutionContext"/> from the workflow execution context.
    /// <param name="predicate">The predicate used to filter the activity execution contexts to remove.</param>
    public void RemoveActivityExecutionContexts(Func<ActivityExecutionContext, bool> predicate)
    {
        var itemsToRemove = _activityExecutionContexts.Where(predicate).ToList();
        foreach (var item in itemsToRemove)
            RemoveActivityExecutionContext(item);
    }

    /// <summary>
    /// Removes all completed activity execution contexts that have a parent activity execution context.
    /// </summary>
    public void ClearCompletedActivityExecutionContexts()
    {
        RemoveActivityExecutionContexts(x => x is { IsCompleted: true, ParentActivityExecutionContext: not null });
    }

    public IEnumerable<ActivityExecutionContext> GetActiveActivityExecutionContexts()
    {
        // Filter out completed activity execution contexts, except for the root Workflow activity context, which stores workflow-level variables.
        // This will currently break scripts accessing activity output directly, but there's a workaround for that via variable capturing.
        // We may ultimately restore direct output access, but in a different way.
        return ActivityExecutionContexts.Where(x => !x.IsCompleted || x.ParentActivityExecutionContext == null);
    }

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
            WorkflowSubStatus.Pending => WorkflowStatus.Running,
            WorkflowSubStatus.Cancelled => WorkflowStatus.Finished,
            WorkflowSubStatus.Executing => WorkflowStatus.Running,
            WorkflowSubStatus.Faulted => WorkflowStatus.Finished,
            WorkflowSubStatus.Finished => WorkflowStatus.Finished,
            WorkflowSubStatus.Suspended => WorkflowStatus.Running,
            _ => throw new ArgumentOutOfRangeException(nameof(subStatus), subStatus, null)
        };

    // TODO: Check if we should not use the target subStatus here instead.
    private bool ValidateStatusTransition()
    {
        var currentMainStatus = GetMainStatus(SubStatus);
        return currentMainStatus != WorkflowStatus.Finished;
    }

    public Task CommitAsync()
    {
        return _commitStateHandler.CommitAsync(this, CancellationToken);
    }
}