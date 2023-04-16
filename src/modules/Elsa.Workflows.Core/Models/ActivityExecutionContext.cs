using System.Collections.ObjectModel;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Represents the context of an activity execution.
/// </summary>
public class ActivityExecutionContext : IExecutionContext
{
    private readonly List<Bookmark> _bookmarks = new();
    private long _executionCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityExecutionContext"/> class.
    /// </summary>
    public ActivityExecutionContext(
        WorkflowExecutionContext workflowExecutionContext,
        ActivityExecutionContext? parentActivityExecutionContext,
        ExpressionExecutionContext expressionExecutionContext,
        IActivity activity,
        ActivityDescriptor activityDescriptor,
        CancellationToken cancellationToken)
    {
        WorkflowExecutionContext = workflowExecutionContext;
        ParentActivityExecutionContext = parentActivityExecutionContext;
        ExpressionExecutionContext = expressionExecutionContext;
        Activity = activity;
        ActivityDescriptor = activityDescriptor;
        CancellationToken = cancellationToken;
        Id = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// The ID of the current activity execution context.
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// The workflow execution context. 
    /// </summary>
    public WorkflowExecutionContext WorkflowExecutionContext { get; }
    
    /// <summary>
    /// The parent activity execution context, if any. 
    /// </summary>
    public ActivityExecutionContext? ParentActivityExecutionContext { get; internal set; }
    
    /// <summary>
    /// The expression execution context.
    /// </summary>
    public ExpressionExecutionContext ExpressionExecutionContext { get; }

    /// <inheritdoc />
    public IEnumerable<Variable> Variables => (Activity as IVariableContainer)?.Variables ?? Enumerable.Empty<Variable>();

    /// <summary>
    /// The currently executing activity.
    /// </summary>
    public IActivity Activity { get; set; }

    /// <summary>
    /// The activity descriptor.
    /// </summary>
    public ActivityDescriptor ActivityDescriptor { get; }

    /// <summary>
    /// A cancellation token to use when invoking asynchronous operations.
    /// </summary>
    public CancellationToken CancellationToken { get; }
    
    /// <inheritdoc />
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// A transient dictionary of values that can be associated with this activity execution context.
    /// These properties only exist while the activity executes and are not persisted. 
    /// </summary>
    public IDictionary<object, object> TransientProperties { get; set; } = new Dictionary<object, object>();

    /// <summary>
    /// Returns the <see cref="ActivityNode"/> metadata about the current activity.
    /// </summary>
    public ActivityNode ActivityNode => WorkflowExecutionContext.FindNodeByActivity(Activity);

    /// <summary>
    /// Returns the global node ID for the current activity within the graph.
    /// </summary>
    public string NodeId => ActivityNode.NodeId;

    /// <summary>
    /// A list of bookmarks created by the current activity.
    /// </summary>
    public IReadOnlyCollection<Bookmark> Bookmarks => new ReadOnlyCollection<Bookmark>(_bookmarks);

    /// <summary>
    /// The number of times this <see cref="ActivityExecutionContext"/> has executed.
    /// </summary>
    public long ExecutionCount => _executionCount;

    /// <summary>
    /// Gets or sets a value that indicates if the workflow should continue executing or not.
    /// </summary>
    public bool Continue { get; private set; } = true;

    /// <summary>
    /// A dictionary of received inputs.
    /// </summary>
    public IDictionary<string, object> Input => WorkflowExecutionContext.Input;

    /// <summary>
    /// Journal data will be added to the workflow execution log for the "Executed" event.  
    /// </summary>
    // ReSharper disable once CollectionNeverQueried.Global
    public IDictionary<string, object?> JournalData { get; } = new Dictionary<string, object?>();

    public ResumedBookmarkContext? ResumedBookmarkContext => WorkflowExecutionContext.ResumedBookmarkContext;
    
    /// <summary>
    /// Stores the evaluated inputs for the current activity.
    /// </summary>
    public IDictionary<string, object> ActivityState { get; set; } = new Dictionary<string, object>();

    public async ValueTask ScheduleActivityAsync(IActivity? activity, ActivityCompletionCallback? completionCallback = default, object? tag = default)
    {
        var activityNode = activity != null ? WorkflowExecutionContext.FindNodeByActivity(activity) : default;
        await ScheduleActivityAsync(activityNode, completionCallback, tag);
    }
    
    public async ValueTask ScheduleActivityAsync(IActivity? activity, ActivityExecutionContext owner, ActivityCompletionCallback? completionCallback = default, object? tag = default)
    {
        var activityNode = activity != null ? WorkflowExecutionContext.FindNodeByActivity(activity) : default;
        await ScheduleActivityAsync(activityNode, this, completionCallback, tag);
    }
    
    public async ValueTask ScheduleActivityAsync(ActivityNode? activityNode, ActivityCompletionCallback? completionCallback = default, object? tag = default)
    {
        await ScheduleActivityAsync(activityNode, this, completionCallback, tag);
    }

    public async ValueTask ScheduleActivityAsync(ActivityNode? activityNode, ActivityExecutionContext owner, ActivityCompletionCallback? completionCallback = default, object? tag = default)
    {
        if (activityNode == null)
        {
            if (completionCallback != null)
                await completionCallback(this, this);
            else
                await owner.CompleteActivityAsync();
            return;
        }

        WorkflowExecutionContext.Schedule(activityNode, owner, completionCallback, tag);
    }

    public async ValueTask ScheduleActivitiesAsync(params IActivity?[] activities) => await ScheduleActivities(activities);

    public async ValueTask ScheduleActivities(IEnumerable<IActivity?> activities, ActivityCompletionCallback? completionCallback = default)
    {
        foreach (var activity in activities)
            await ScheduleActivityAsync(activity, completionCallback);
    }

    public void CreateBookmarks(IEnumerable<object> payloads, ExecuteActivityDelegate? callback = default)
    {
        foreach (var payload in payloads)
            CreateBookmark(new BookmarkOptions(payload, callback));
    }

    public void AddBookmarks(IEnumerable<Bookmark> bookmarks) => _bookmarks.AddRange(bookmarks);
    public void AddBookmark(Bookmark bookmark) => _bookmarks.Add(bookmark);

    public Bookmark CreateBookmark(ExecuteActivityDelegate callback) => CreateBookmark(new BookmarkOptions(default, callback));
    public Bookmark CreateBookmark(object payload, ExecuteActivityDelegate callback) => CreateBookmark(new BookmarkOptions(payload, callback));
    public Bookmark CreateBookmark(object payload) => CreateBookmark(new BookmarkOptions(payload));

    /// <summary>
    /// Creates a bookmark so that this activity can be resumed at a later time.
    /// Creating a bookmark will automatically suspend the workflow after all pending activities have executed.
    /// </summary>
    public Bookmark CreateBookmark(BookmarkOptions? options = default)
    {
        var payload = options?.Payload;
        var callback = options?.Callback;
        var bookmarkName = options?.BookmarkName ?? Activity.Type;
        var bookmarkHasher = GetRequiredService<IBookmarkHasher>();
        var identityGenerator = GetRequiredService<IIdentityGenerator>();
        var payloadSerializer = GetRequiredService<IBookmarkPayloadSerializer>();
        //var payloadJson = payload != null ? payloadSerializer.Serialize(payload) : default;
        var hash = bookmarkHasher.Hash(bookmarkName, payload);

        var bookmark = new Bookmark(
            identityGenerator.GenerateId(),
            bookmarkName,
            hash,
            payload,
            ActivityNode.NodeId,
            Id,
            options?.AutoBurn ?? true,
            callback?.Method.Name);

        AddBookmark(bookmark);
        return bookmark;
    }

    /// <summary>
    /// Clear all bookmarks.
    /// </summary>
    public void ClearBookmarks() => _bookmarks.Clear();

    /// <summary>
    /// Returns a property value associated with the current activity context. 
    /// </summary>
    public T? GetProperty<T>(string key) => Properties.TryGetValue<T?>(key, out var value) ? value : default;

    /// <summary>
    /// Returns a property value associated with the current activity context. 
    /// </summary>
    public T GetProperty<T>(string key, Func<T> defaultValue)
    {
        if (Properties.TryGetValue<T?>(key, out var value))
            return value!;

        value = defaultValue();
        Properties[key] = value!;

        return value!;
    }

    /// <summary>
    /// Stores a property associated with the current activity context. 
    /// </summary>
    public void SetProperty<T>(string key, T? value) => Properties[key] = value!;

    /// <summary>
    /// Updates a property associated with the current activity context. 
    /// </summary>
    public T UpdateProperty<T>(string key, Func<T?, T> updater) where T : notnull
    {
        var value = GetProperty<T?>(key);
        value = updater(value);
        Properties[key] = value;
        return value;
    }
    
    /// <summary>
    /// Removes a property associated with the current activity context.
    /// </summary>
    /// <param name="key">The property key.</param>
    public void RemoveProperty(string key) => Properties.Remove(key);

    public T GetRequiredService<T>() where T : notnull => WorkflowExecutionContext.GetRequiredService<T>();
    public object GetRequiredService(Type serviceType) => WorkflowExecutionContext.GetRequiredService(serviceType);
    public T GetOrCreateService<T>() where T : notnull => WorkflowExecutionContext.GetOrCreateService<T>();
    public object GetOrCreateService(Type serviceType) => WorkflowExecutionContext.GetOrCreateService(serviceType);
    public T? GetService<T>() where T : notnull => WorkflowExecutionContext.GetService<T>();
    public IEnumerable<T> GetServices<T>() where T : notnull => WorkflowExecutionContext.GetServices<T>();
    public object? GetService(Type serviceType) => WorkflowExecutionContext.GetService(serviceType);
    public T? Get<T>(Input<T>? input) => input == null ? default : Get<T>(input.MemoryBlockReference());

    public object? Get(MemoryBlockReference blockReference)
    {
        var location = GetMemoryBlock(blockReference) ?? throw new InvalidOperationException($"No location found with ID {blockReference.Id}. Did you forget to declare a variable with a container?");
        return location.Value;
    }

    public T? Get<T>(MemoryBlockReference blockReference)
    {
        var value = Get(blockReference);
        return value != default ? value.ConvertTo<T>() : default;
    }

    public void Set(MemoryBlockReference blockReference, object? value, Action<MemoryBlock>? configure = default) => ExpressionExecutionContext.Set(blockReference, value, configure);
    public void Set(Output? output, object? value) => ExpressionExecutionContext.Set(output, value);
    public void Set<T>(Output<T>? output, T? value) => ExpressionExecutionContext.Set(output, value);

    /// <summary>
    /// Stops further execution of the workflow.
    /// </summary>
    public void PreventContinuation() => Continue = false;

    /// <summary>
    /// Removes all completion callbacks for the current activity.
    /// </summary>
    public void ClearCompletionCallbacks()
    {
        var entriesToRemove = WorkflowExecutionContext.CompletionCallbacks.Where(x => x.Owner == this);
        WorkflowExecutionContext.RemoveCompletionCallbacks(entriesToRemove);
    }

    internal void IncrementExecutionCount() => _executionCount++;

    private MemoryBlock? GetMemoryBlock(MemoryBlockReference locationBlockReference) =>
        ExpressionExecutionContext.Memory.TryGetBlock(locationBlockReference.Id, out var memoryBlock)
            ? memoryBlock
            : ParentActivityExecutionContext?.GetMemoryBlock(locationBlockReference);
}