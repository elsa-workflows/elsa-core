using System.Collections.ObjectModel;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Models;

public record ActivityCompletionCallbackEntry(ActivityExecutionContext Owner, IActivity Child, ActivityCompletionCallback CompletionCallback);

public class WorkflowExecutionContext
{
    internal static ValueTask Noop(ActivityExecutionContext context) => new();
    internal static ValueTask Complete(ActivityExecutionContext context) => context.CompleteActivityAsync();
    private readonly IServiceProvider _serviceProvider;
    private readonly IList<ActivityNode> _nodes;
    private readonly IList<ActivityCompletionCallbackEntry> _completionCallbackEntries = new List<ActivityCompletionCallbackEntry>();

    public WorkflowExecutionContext(
        IServiceProvider serviceProvider,
        string id,
        string? correlationId,
        Workflow workflow,
        ActivityNode graph,
        IActivityScheduler scheduler,
        IDictionary<string, object>? input,
        ExecuteActivityDelegate? executeDelegate,
        CancellationToken cancellationToken)
    {
        _serviceProvider = serviceProvider;
        Workflow = workflow;
        Graph = graph;
        SubStatus = WorkflowSubStatus.Executing;
        Id = id;
        CorrelationId = correlationId;
        _nodes = graph.Flatten().Distinct().ToList();
        Scheduler = scheduler;
        Input = input ?? new Dictionary<string, object>();
        ExecuteDelegate = executeDelegate;
        CancellationToken = cancellationToken;
        NodeIdLookup = _nodes.ToDictionary(x => x.NodeId);
        NodeActivityLookup = _nodes.ToDictionary(x => x.Activity);
        MemoryRegister = workflow.CreateRegister();
    }

    public Workflow Workflow { get; }
    public ActivityNode Graph { get; }
    public WorkflowStatus Status => GetMainStatus(SubStatus);
    public WorkflowSubStatus SubStatus { get; internal set; }
    public MemoryRegister MemoryRegister { get; }
    public string Id { get; set; }
    public string? CorrelationId { get; set; }
    public IReadOnlyCollection<ActivityNode> Nodes => new ReadOnlyCollection<ActivityNode>(_nodes);
    public IDictionary<string, ActivityNode> NodeIdLookup { get; }
    public IDictionary<IActivity, ActivityNode> NodeActivityLookup { get; }
    public IActivityScheduler Scheduler { get; }
    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
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

    public ExecuteActivityDelegate? ExecuteDelegate { get; set; }
    public CancellationToken CancellationToken { get; }
    public ICollection<ActivityCompletionCallbackEntry> CompletionCallbacks => new ReadOnlyCollection<ActivityCompletionCallbackEntry>(_completionCallbackEntries);
    public ICollection<ActivityExecutionContext> ActivityExecutionContexts { get; set; } = new List<ActivityExecutionContext>();

    /// <summary>
    /// A volatile collection of executed activity instance IDs. This collection is reset when workflow execution starts.
    /// </summary>
    public ICollection<WorkflowExecutionLogEntry> ExecutionLog { get; } = new List<WorkflowExecutionLogEntry>();

    public T GetRequiredService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();
    public object GetRequiredService(Type serviceType) => _serviceProvider.GetRequiredService(serviceType);
    public T GetOrCreateService<T>() where T : notnull => ActivatorUtilities.GetServiceOrCreateInstance<T>(_serviceProvider);
    public object GetOrCreateService(Type serviceType) => ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, serviceType);
    public T? GetService<T>() where T : notnull => _serviceProvider.GetService<T>();
    public object? GetService(Type serviceType) => _serviceProvider.GetService(serviceType);

    public void AddCompletionCallback(ActivityExecutionContext owner, IActivity child, ActivityCompletionCallback completionCallback)
    {
        var entry = new ActivityCompletionCallbackEntry(owner, child, completionCallback);
        _completionCallbackEntries.Add(entry);
    }

    public ActivityCompletionCallback? PopCompletionCallback(ActivityExecutionContext owner, IActivity child)
    {
        var entry = _completionCallbackEntries.FirstOrDefault(x => x.Owner == owner && x.Child == child);

        if (entry == null)
            return default;

        RemoveCompletionCallback(entry);
        return entry.CompletionCallback;
    }

    public void RemoveCompletionCallback(ActivityCompletionCallbackEntry entry) => _completionCallbackEntries.Remove(entry);

    public void RemoveCompletionCallbacks(IEnumerable<ActivityCompletionCallbackEntry> entries)
    {
        foreach (var entry in entries.ToList())
            _completionCallbackEntries.Remove(entry);
    }

    public ActivityNode FindActivityNodeById(string nodeId) => NodeIdLookup[nodeId];
    public ActivityNode FindNodeByActivity(IActivity activity) => NodeActivityLookup[activity];
    public IActivity FindActivityById(string activityId) => FindActivityNodeById(activityId).Activity;
    public T? GetProperty<T>(string key) => Properties.TryGetValue(key, out var value) ? (T?)value : default(T);
    public void SetProperty<T>(string key, T value) => Properties[key] = value!;

    public T UpdateProperty<T>(string key, Func<T?, T> updater)
    {
        var value = GetProperty<T?>(key);
        value = updater(value);
        Properties[key] = value!;
        return value;
    }

    
    public void TransitionTo(WorkflowSubStatus subStatus)
    {
        var targetStatus = GetMainStatus(subStatus);

        if (!ValidateStatusTransition(SubStatus, subStatus))
            throw new Exception($"Cannot transition from {Status} to {targetStatus}");

        SubStatus = subStatus;
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