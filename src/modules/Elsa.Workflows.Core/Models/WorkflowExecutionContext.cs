using System.Collections.ObjectModel;
using Elsa.Common.Extensions;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// A delegate entry that is used by activities to be notified when the activities they scheduled are completed.
/// </summary>
/// <param name="Owner">The activity scheduling the <see cref="Child"/> activity.</param>
/// <param name="Child">The child <see cref="IActivity"/> being scheduled.</param>
/// <param name="CompletionCallback">The <see cref="ActivityCompletionCallback"/> delegate to invoke when the scheduled <see cref="Child"/> activity completes.</param>
public record ActivityCompletionCallbackEntry(ActivityExecutionContext Owner, IActivity Child, ActivityCompletionCallback? CompletionCallback);

/// <summary>
/// Provides context to the currently executing workflow.
/// </summary>
public class WorkflowExecutionContext
{
    internal static ValueTask Complete(ActivityExecutionContext context) => context.CompleteActivityAsync();
    private readonly IServiceProvider _serviceProvider;
    private readonly IList<ActivityNode> _nodes;
    private readonly IList<ActivityCompletionCallbackEntry> _completionCallbackEntries = new List<ActivityCompletionCallbackEntry>();
    private IList<ActivityExecutionContext> _activityExecutionContexts;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowExecutionContext(
        IServiceProvider serviceProvider,
        string id,
        string? correlationId,
        Workflow workflow,
        ActivityNode graph,
        ICollection<ActivityNode> nodes,
        IActivityScheduler scheduler,
        IDictionary<string, object>? input,
        ExecuteActivityDelegate? executeDelegate,
        string? triggerActivityId,
        IEnumerable<ActivityExecutionContext>? activityExecutionContexts,
        CancellationToken cancellationToken)
    {
        _serviceProvider = serviceProvider;
        Workflow = workflow;
        Graph = graph;
        SubStatus = WorkflowSubStatus.Executing;
        Id = id;
        CorrelationId = correlationId;
        _nodes = nodes.ToList();
        _activityExecutionContexts = activityExecutionContexts?.ToList() ?? new List<ActivityExecutionContext>();
        Scheduler = scheduler;
        Input = input ?? new Dictionary<string, object>();
        ExecuteDelegate = executeDelegate;
        TriggerActivityId = triggerActivityId;
        CancellationToken = cancellationToken;
        NodeIdLookup = _nodes.ToDictionary(x => x.NodeId);
        NodeActivityLookup = _nodes.ToDictionary(x => x.Activity);
        MemoryRegister = workflow.CreateRegister();
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
    /// A flattened list of <see cref="ActivityNode"/>s from the <see cref="Graph"/>. 
    /// </summary>
    public IReadOnlyCollection<ActivityNode> Nodes => new ReadOnlyCollection<ActivityNode>(_nodes);
    
    /// <summary>
    /// A map between activity IDs and <see cref="ActivityNode"/>s in the workflow graph.
    /// </summary>
    public IDictionary<string, ActivityNode> NodeIdLookup { get; }
    
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
    /// A dictionary that can be used by application code and activities to store information. Values need to be serializable, since this dictionary will be persisted alongside the workflow instance. 
    /// </summary>
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// A dictionary that can be used by application code and middleware to store information and even services. Values do not need to be serializable, since this dictionary will not be persisted.
    /// All data will be gone once workflow execution completes. 
    /// </summary>
    public IDictionary<object, object> TransientProperties { get; set; } = new Dictionary<object, object>();

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
    public IReadOnlyCollection<ActivityExecutionContext> ActivityExecutionContexts
    {
        get => _activityExecutionContexts.ToList();
        internal set => _activityExecutionContexts = value.ToList();
    }

    /// <summary>
    /// A volatile collection of executed activity instance IDs. This collection is reset when workflow execution starts.
    /// </summary>
    public ICollection<WorkflowExecutionLogEntry> ExecutionLog { get; } = new List<WorkflowExecutionLogEntry>();

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
    internal void AddCompletionCallback(ActivityExecutionContext owner, IActivity child, ActivityCompletionCallback? completionCallback = default)
    {
        var entry = new ActivityCompletionCallbackEntry(owner, child, completionCallback);
        _completionCallbackEntries.Add(entry);
    }

    /// <summary>
    /// Unregisters the completion callback for the specified owner and child activity.
    /// </summary>
    internal ActivityCompletionCallbackEntry? PopCompletionCallback(ActivityExecutionContext owner, IActivity child)
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
    public ActivityNode FindNodeById(string activityId) => NodeIdLookup[activityId];
    
    /// <summary>
    /// Returns the <see cref="ActivityNode"/> containing the specified activity from the workflow graph.
    /// </summary>
    public ActivityNode FindNodeByActivity(IActivity activity) => NodeActivityLookup[activity];
    
    /// <summary>
    /// Returns the <see cref="IActivity"/> with the specified ID from the workflow graph.
    /// </summary>
    public IActivity FindActivityById(string activityId) => FindNodeById(activityId).Activity;
    
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

    internal void TransitionTo(WorkflowSubStatus subStatus)
    {
        var targetStatus = GetMainStatus(subStatus);

        if (!ValidateStatusTransition(SubStatus, subStatus))
            throw new Exception($"Cannot transition from {Status} to {targetStatus}");

        SubStatus = subStatus;
    }

    /// <summary>
    /// Creates a new <see cref="ActivityExecutionContext"/> for the specified activity.
    /// </summary>
    public ActivityExecutionContext CreateActivityExecutionContext(IActivity activity, ActivityExecutionContext? parentContext = default)
    {
        var parentExpressionExecutionContext = parentContext?.ExpressionExecutionContext;
        var properties = ExpressionExecutionContextExtensions.CreateActivityExecutionContextPropertiesFrom(this, Input);
        var parentMemory = parentContext?.ExpressionExecutionContext.Memory ?? MemoryRegister;
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, parentMemory, parentExpressionExecutionContext, properties, CancellationToken);
        var activityExecutionContext = new ActivityExecutionContext(this, parentContext, expressionExecutionContext, activity, CancellationToken);
        expressionExecutionContext.TransientProperties[ExpressionExecutionContextExtensions.ActivityExecutionContextKey] = activityExecutionContext;
        return activityExecutionContext;
    }
    
    /// <summary>
    /// Removes the specified <see cref="ActivityExecutionContext"/>.
    /// </summary>
    internal async Task RemoveActivityExecutionContextAsync(ActivityExecutionContext context)
    {
        // Remove all contexts referencing this one as a parent.
        var childContexts = _activityExecutionContexts.Where(x => x.ParentActivityExecutionContext == context).ToList();

        foreach (var childContext in childContexts) await RemoveActivityExecutionContextAsync(childContext);

        // Remove the context.
        _activityExecutionContexts.Remove(context);
        
        // Remove all associated completion callbacks.
        context.ClearCompletionCallbacks();
        
        // Remove all associated variables.
        var variablePersistenceManager = context.GetRequiredService<IVariablePersistenceManager>();
        var variables = variablePersistenceManager.GetVariables(context);
        await variablePersistenceManager.DeleteVariablesAsync(this, variables);
        
        // Remove all associated bookmarks.
        Bookmarks.RemoveWhere(x => x.ActivityInstanceId == context.Id);
    }

    /// <summary>
    /// Adds the specified <see cref="ActivityExecutionContext"/> to the workflow execution context.
    /// </summary>
    internal void AddActivityExecutionContext(ActivityExecutionContext context) => _activityExecutionContexts.Add(context);

    /// <summary>
    /// Cancels the specified activity. 
    /// </summary>
    public async Task CancelActivityAsync(string activityId)
    {
        var activityExecutionContext = ActivityExecutionContexts.FirstOrDefault(x => x.Id == activityId);

        if (activityExecutionContext != null) 
            await activityExecutionContext.CancelActivityAsync();

        Bookmarks.RemoveWhere(x => x.ActivityId == activityId);
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

    private bool ValidateStatusTransition(WorkflowSubStatus currentSubStatus, WorkflowSubStatus target)
    {
        var currentMainStatus = GetMainStatus(currentSubStatus);
        return currentMainStatus != WorkflowStatus.Finished;
    }

    private IEnumerable<MemoryRegister> GetMergedRegistersView() => new[] { MemoryRegister }.Concat(ActivityExecutionContexts.Select(x => x.ExpressionExecutionContext.Memory)).ToList();
}