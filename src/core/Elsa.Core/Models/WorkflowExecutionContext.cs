using System.Collections.ObjectModel;
using Elsa.Contracts;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Models;

//public record ActivityCompletionCallbackEntry(ActivityExecutionContext Owner, IActivity Child, ActivityCompletionCallback CompletionCallback);
public record ActivityCompletionCallbackEntry(ActivityExecutionContext Owner, IActivity Child, ActivityCompletionCallback CompletionCallback);

public class WorkflowExecutionContext
{
    private static ValueTask Noop(ActivityExecutionContext context) => new();
    private readonly IServiceProvider _serviceProvider;
    private readonly IList<ActivityNode> _nodes;
    private readonly IList<ActivityCompletionCallbackEntry> _completionCallbackEntries = new List<ActivityCompletionCallbackEntry>();
    private readonly List<Bookmark> _bookmarks = new();

    public WorkflowExecutionContext(
        IServiceProvider serviceProvider,
        Workflow workflow,
        ActivityNode graph,
        IActivityScheduler scheduler,
        Bookmark? bookmark,
        ExecuteActivityDelegate? executeDelegate,
        CancellationToken cancellationToken)
    {
        _serviceProvider = serviceProvider;
        Workflow = workflow;
        Graph = graph;
        Id = Guid.NewGuid().ToString("N");
        _nodes = graph.Flatten().Distinct().ToList();
        Scheduler = scheduler;
        Bookmark = bookmark;
        ExecuteDelegate = executeDelegate;
        CancellationToken = cancellationToken;
        NodeIdLookup = _nodes.ToDictionary(x => x.NodeId);
        NodeActivityLookup = _nodes.ToDictionary(x => x.Activity);
    }

    public Workflow Workflow { get; }
    public ActivityNode Graph { get; set; }
    public string Id { get; set; }
    public IReadOnlyCollection<ActivityNode> Nodes => new ReadOnlyCollection<ActivityNode>(_nodes);
    public IDictionary<string, ActivityNode> NodeIdLookup { get; }
    public IDictionary<IActivity, ActivityNode> NodeActivityLookup { get; }
    public IActivityScheduler Scheduler { get; }
    public Bookmark? Bookmark { get; }
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
    public ExecuteActivityDelegate? ExecuteDelegate { get; set; }
    public CancellationToken CancellationToken { get; }
    public IReadOnlyCollection<Bookmark> Bookmarks => new ReadOnlyCollection<Bookmark>(_bookmarks);
    public IReadOnlyCollection<ActivityCompletionCallbackEntry> CompletionCallbacks => new ReadOnlyCollection<ActivityCompletionCallbackEntry>(_completionCallbackEntries);
    public ICollection<ActivityExecutionContext> ActivityExecutionContexts { get; set; } = new List<ActivityExecutionContext>();

    /// <summary>
    /// A volatile collection of executed activity instance IDs. This collection is reset when workflow execution starts.
    /// </summary>
    public ICollection<WorkflowExecutionLogEntry> ExecutionLog { get; } = new List<WorkflowExecutionLogEntry>();

    public T GetRequiredService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();

    public void Schedule(IActivity activity, ActivityExecutionContext owner, ActivityCompletionCallback? completionCallback = default, IEnumerable<RegisterLocationReference>? locationReferences = default, object? tag = default)
    {
        var activityInvoker = GetRequiredService<IActivityInvoker>();
        var workItem = new ActivityWorkItem(activity.Id, async () => await activityInvoker.InvokeAsync(this, activity, owner, locationReferences), tag);
        Scheduler.Push(workItem);

        if (completionCallback != null)
            AddCompletionCallback(owner, activity, completionCallback);
    }

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

        _completionCallbackEntries.Remove(entry);
        return entry.CompletionCallback;
    }

    public ActivityNode FindNodeById(string nodeId) => NodeIdLookup[nodeId];
    public ActivityNode FindNodeByActivity(IActivity activity) => NodeActivityLookup[activity];
    public IActivity FindActivityById(string activityId) => FindNodeById(activityId).Activity;
    public T? GetProperty<T>(string key) => Properties.TryGetValue(key, out var value) ? (T?)value : default(T);
    public void SetProperty<T>(string key, T value) => Properties[key] = value;

    public T UpdateProperty<T>(string key, Func<T?, T> updater)
    {
        var value = GetProperty<T?>(key);
        value = updater(value);
        Properties[key] = value;
        return value;
    }

    public void RegisterBookmarks(IEnumerable<Bookmark> bookmarks) => _bookmarks.AddRange(bookmarks);

    public void UnregisterBookmarks(IEnumerable<Bookmark> bookmarks)
    {
        foreach (var bookmark in bookmarks)
            _bookmarks.Remove(bookmark);
    }

    public void ScheduleRoot()
    {
        var activityInvoker = GetRequiredService<IActivityInvoker>();
        var workItem = new ActivityWorkItem(Workflow.Root.Id, async () => await activityInvoker.InvokeAsync(this, Workflow.Root));
        Scheduler.Push(workItem);
    }

    public void ScheduleBookmark(Bookmark bookmark)
    {
        // Construct bookmark.
        var bookmarkedActivityContext = ActivityExecutionContexts.First(x => x.Id == bookmark.ActivityInstanceId);
        var bookmarkedActivity = bookmarkedActivityContext.Activity;

        // Schedule the activity to resume.
        var activityInvoker = GetRequiredService<IActivityInvoker>();
        var workItem = new ActivityWorkItem(bookmarkedActivity.Id, async () => await activityInvoker.InvokeAsync(bookmarkedActivityContext));
        Scheduler.Push(workItem);
        
        // If no resumption point was specified, use Noop to prevent the regular "ExecuteAsync" method to be invoked.
        ExecuteDelegate = bookmark.CallbackMethodName != null ? bookmarkedActivity.GetResumeActivityDelegate(bookmark.CallbackMethodName) : Noop;
    }
}