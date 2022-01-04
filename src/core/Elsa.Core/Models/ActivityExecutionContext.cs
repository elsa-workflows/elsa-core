using System.Collections.ObjectModel;
using Elsa.Contracts;

namespace Elsa.Models;

public class ActivityExecutionContext
{
    private readonly List<Bookmark> _bookmarks = new();

    public ActivityExecutionContext(
        WorkflowExecutionContext workflowExecutionContext,
        ActivityExecutionContext? parentActivityExecutionContext,
        ExpressionExecutionContext expressionExecutionContext,
        IActivity activity,
        CancellationToken cancellationToken)
    {
        WorkflowExecutionContext = workflowExecutionContext;
        ParentActivityExecutionContext = parentActivityExecutionContext;
        ExpressionExecutionContext = expressionExecutionContext;
        Activity = activity;
        CancellationToken = cancellationToken;
        Id = Guid.NewGuid().ToString();
    }

    public string Id { get; set; }
    public WorkflowExecutionContext WorkflowExecutionContext { get; }
    public ActivityExecutionContext? ParentActivityExecutionContext { get; internal set; }
    public ExpressionExecutionContext ExpressionExecutionContext { get; }
    public IActivity Activity { get; set; }
    public CancellationToken CancellationToken { get; }
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
    public ActivityNode ActivityNode => WorkflowExecutionContext.FindNodeByActivity(Activity);
    public IReadOnlyCollection<Bookmark> Bookmarks => new ReadOnlyCollection<Bookmark>(_bookmarks);
    public bool Continue { get; set; } = true;

    public void ScheduleActivity(IActivity activity, ActivityCompletionCallback? completionCallback = default, IEnumerable<RegisterLocationReference>? locationReferences = default, object? tag = default) =>
        WorkflowExecutionContext.Schedule(activity, this, completionCallback, locationReferences, tag);

    public void ScheduleActivity(IActivity activity, ActivityExecutionContext owner, ActivityCompletionCallback? completionCallback = default, IEnumerable<RegisterLocationReference>? locationReferences = default, object? tag = default) =>
        WorkflowExecutionContext.Schedule(activity, owner, completionCallback, locationReferences, tag);

    public void ScheduleActivities(params IActivity[] activities) => ScheduleActivities((IEnumerable<IActivity>)activities);

    public void ScheduleActivities(IEnumerable<IActivity> activities, ActivityCompletionCallback? completionCallback = default)
    {
        foreach (var activity in activities)
            ScheduleActivity(activity, completionCallback);
    }

    public void SetBookmarks(IEnumerable<Bookmark> bookmarks) => _bookmarks.AddRange(bookmarks);
    public void SetBookmark(Bookmark bookmark) => _bookmarks.Add(bookmark);

    public void SetBookmark(string? hash, IDictionary<string, object?>? data = default, ExecuteActivityDelegate? callback = default) =>
        SetBookmark(new Bookmark(
            Guid.NewGuid().ToString(),
            Activity.NodeType,
            hash,
            Activity.Id,
            Id,
            data ?? new Dictionary<string, object?>(),
            callback?.Method.Name));

    public T? GetProperty<T>(string key) => Properties.TryGetValue(key, out var value) ? (T?)value : default;
    public void SetProperty<T>(string key, T value) => Properties[key] = value;

    public T UpdateProperty<T>(string key, Func<T?, T> updater)
    {
        var value = GetProperty<T?>(key);
        value = updater(value);
        Properties[key] = value;
        return value;
    }

    public T GetRequiredService<T>() where T : notnull => WorkflowExecutionContext.GetRequiredService<T>();
    public T? Get<T>(Input<T> input) => Get<T>(input.LocationReference);

    public object? Get(RegisterLocationReference locationReference)
    {
        var location = GetLocation(locationReference) ?? throw new InvalidOperationException($"No location found with ID {locationReference.Id}. Did you forget to declare a variable with a container?");
        return location.Value;
    }

    public T? Get<T>(RegisterLocationReference locationReference)
    {
        var value = Get(locationReference);
        return value != default ? (T?)(value) : default;
    }

    public void Set(RegisterLocationReference locationReference, object? value) => ExpressionExecutionContext.Set(locationReference, value);
    public void Set(Output? output, object? value) => ExpressionExecutionContext.Set(output, value);

    public async Task<T?> EvaluateAsync<T>(Input<T> input)
    {
        var evaluator = GetRequiredService<IExpressionEvaluator>();
        var locationReference = input.LocationReference;
        var value = await evaluator.EvaluateAsync(input, ExpressionExecutionContext);
        locationReference.Set(this, value);
        return (T?)value;
    }

    private RegisterLocation? GetLocation(RegisterLocationReference locationReference) =>
        ExpressionExecutionContext.Register.TryGetLocation(locationReference.Id, out var location)
            ? location
            : ParentActivityExecutionContext?.GetLocation(locationReference);

    public void PreventContinuation() => Continue = false;
}