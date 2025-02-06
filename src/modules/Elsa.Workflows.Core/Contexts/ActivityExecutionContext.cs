using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Elsa.Common;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using JetBrains.Annotations;

namespace Elsa.Workflows;

/// <summary>
/// Represents the context of an activity execution.
/// </summary>
[PublicAPI]
public partial class ActivityExecutionContext : IExecutionContext, IDisposable
{
    private readonly ISystemClock _systemClock;
    private readonly List<Bookmark> _bookmarks = [];
    private ActivityStatus _status;
    private Exception? _exception;
    private long _executionCount;
    private ActivityExecutionContext? _parentActivityExecutionContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityExecutionContext"/> class.
    /// </summary>
    public ActivityExecutionContext(
        string id,
        WorkflowExecutionContext workflowExecutionContext,
        ActivityExecutionContext? parentActivityExecutionContext,
        IActivity activity,
        ActivityDescriptor activityDescriptor,
        DateTimeOffset startedAt,
        object? tag,
        ISystemClock systemClock,
        CancellationToken cancellationToken)
    {
        _systemClock = systemClock;
        Properties = new ChangeTrackingDictionary<string, object>(Taint);
        ActivityState = new ChangeTrackingDictionary<string, object>(Taint);
        ActivityInput = new ChangeTrackingDictionary<string, object>(Taint);
        WorkflowExecutionContext = workflowExecutionContext;
        _parentActivityExecutionContext = parentActivityExecutionContext;
        var expressionExecutionContextProps = ExpressionExecutionContextExtensions.CreateActivityExecutionContextPropertiesFrom(workflowExecutionContext, workflowExecutionContext.Input);
        expressionExecutionContextProps[ExpressionExecutionContextExtensions.ActivityKey] = activity;
        ExpressionExecutionContext = new(workflowExecutionContext.ServiceProvider, new(), parentActivityExecutionContext?.ExpressionExecutionContext ?? workflowExecutionContext.ExpressionExecutionContext, expressionExecutionContextProps, Taint, CancellationToken);
        ;
        Activity = activity;
        ActivityDescriptor = activityDescriptor;
        StartedAt = startedAt;
        Status = ActivityStatus.Pending;
        Tag = tag;
        CancellationToken = cancellationToken;
        Id = id;
        _publisher = GetRequiredService<INotificationSender>();
    }

    /// <summary>
    /// The ID of the current activity execution context.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The time at which the activity execution context was created.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// An optional tag to associate with the activity execution.
    /// </summary>
    public object? Tag { get; set; }

    /// <summary>
    /// The time at which the activity execution context was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Returns true if the activity execution context has completed.
    /// </summary>
    public bool IsCompleted => Status is ActivityStatus.Completed or ActivityStatus.Canceled;

    /// <summary>
    /// The workflow execution context. 
    /// </summary>
    public WorkflowExecutionContext WorkflowExecutionContext { get; }

    /// <summary>
    /// The parent activity execution context, if any. 
    /// </summary>
    public ActivityExecutionContext? ParentActivityExecutionContext
    {
        get => _parentActivityExecutionContext;
        internal set
        {
            _parentActivityExecutionContext = value;
            _parentActivityExecutionContext?.Children.Add(this);
        }
    }

    /// <summary>
    /// The expression execution context.
    /// </summary>
    public ExpressionExecutionContext ExpressionExecutionContext { get; }

    /// <inheritdoc />
    public IEnumerable<Variable> Variables
    {
        get
        {
            var containerVariables = (Activity as IVariableContainer)?.Variables ?? Enumerable.Empty<Variable>();
            var dynamicVariables = DynamicVariables;
            return containerVariables.Concat(dynamicVariables).DistinctBy(x => x.Name);
        }
    }

    /// <summary>
    /// A list of variables that are dynamically added to the activity execution context.
    /// </summary>
    public ICollection<Variable> DynamicVariables { get; set; } = new List<Variable>();

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

    /// <summary>
    /// The current status of the activity.
    /// </summary>
    public ActivityStatus Status
    {
        get => _status;
        private set
        {
            if (value == _status)
                return;

            _status = value;
            Taint();
        }
    }

    /// <summary>
    /// Sets the current status of the activity.
    /// </summary>
    public void TransitionTo(ActivityStatus status)
    {
        Status = status;
    }

    /// <summary>
    /// Gets or sets the exception that occurred during the activity execution, if any.
    /// </summary>
    public Exception? Exception
    {
        get => _exception;
        set
        {
            _exception = value;
            Taint();
        }
    }

    /// <inheritdoc />
    public IDictionary<string, object> Properties { get; private set; }

    /// <summary>
    /// A transient dictionary of values that can be associated with this activity execution context.
    /// These properties only exist while the activity executes and are not persisted. 
    /// </summary>
    public IDictionary<object, object> TransientProperties { get; set; } = new Dictionary<object, object>();

    /// <summary>
    /// Returns the <see cref="ActivityNode"/> metadata about the current activity.
    /// </summary>
    public ActivityNode ActivityNode => WorkflowExecutionContext.FindNodeByActivity(Activity)!;

    /// <summary>
    /// Returns the global node ID for the current activity within the graph.
    /// </summary>
    /// <remarks>As of tool version 3.0, all activity Ids are already unique, so there's no need to construct a hierarchical ID</remarks>
    public string NodeId => ActivityNode.NodeId;

    public ISet<ActivityExecutionContext> Children { get; } = new HashSet<ActivityExecutionContext>();

    /// <summary>
    /// A list of bookmarks created by the current activity.
    /// </summary>
    public IReadOnlyCollection<Bookmark> Bookmarks => new ReadOnlyCollection<Bookmark>(_bookmarks);

    /// <summary>
    /// The number of times this <see cref="ActivityExecutionContext"/> has executed.
    /// </summary>
    public long ExecutionCount => _executionCount;

    /// <summary>
    /// A dictionary of received inputs for the current workflow.
    /// </summary>
    public IDictionary<string, object> WorkflowInput => WorkflowExecutionContext.Input;

    /// <summary>
    /// A dictionary of inputs for the current activity.
    /// </summary>
    public IDictionary<string, object> ActivityInput { get; private set; }

    /// <summary>
    /// Journal data will be added to the workflow execution log for the "Executed" event.  
    /// </summary>
    // ReSharper disable once CollectionNeverQueried.Global
    public IDictionary<string, object> JournalData { get; } = new Dictionary<string, object>();

    /// <summary>
    /// Stores the evaluated inputs, serialized, for the current activity for historical purposes.
    /// </summary>
    public IDictionary<string, object> ActivityState { get; }

    /// <summary>
    /// Indicates whether the state of the current activity execution context has been modified.
    /// </summary>
    public bool IsDirty { get; private set; }

    /// <summary>
    /// Schedules the specified activity to be executed.
    /// </summary>
    /// <param name="activity">The activity to schedule.</param>
    /// <param name="completionCallback">An optional callback to invoke when the activity completes.</param>
    /// <param name="tag">An optional tag to associate with the activity execution.</param>
    /// <param name="variables">An optional list of variables to declare with the activity execution.</param>
    public ValueTask ScheduleActivityAsync(IActivity? activity, ActivityCompletionCallback? completionCallback, object? tag = null, IEnumerable<Variable>? variables = null)
    {
        var options = new ScheduleWorkOptions
        {
            CompletionCallback = completionCallback,
            Tag = tag,
            Variables = variables?.ToList()
        };
        return ScheduleActivityAsync(activity, options);
    }

    /// <summary>
    /// Schedules the specified activity to be executed.
    /// </summary>
    /// <param name="activity">The activity to schedule.</param>
    /// <param name="options">The options used to schedule the activity.</param>
    public async ValueTask ScheduleActivityAsync(IActivity? activity, ScheduleWorkOptions? options = null)
    {
        await ScheduleActivityAsync(activity, this, options);
    }

    /// <summary>
    /// Schedules the specified activity to be executed.
    /// </summary>
    /// <param name="activity">The activity to schedule.</param>
    /// <param name="owner">The activity execution context that owns the scheduled activity.</param>
    /// <param name="options">The options used to schedule the activity.</param>
    public async ValueTask ScheduleActivityAsync(IActivity? activity, ActivityExecutionContext? owner, ScheduleWorkOptions? options = null)
    {
        var activityNode = activity != null
            ? WorkflowExecutionContext.FindNodeByActivity(activity) ?? throw new InvalidOperationException("The specified activity is not part of the workflow.")
            : null;
        await ScheduleActivityAsync(activityNode, owner, options);
    }

    /// <summary>
    /// Schedules the specified activity to be executed.
    /// </summary>
    /// <param name="activityNode">The activity node to schedule.</param>
    /// <param name="owner">The activity execution context that owns the scheduled activity.</param>
    /// <param name="options">The options used to schedule the activity.</param>
    public async ValueTask ScheduleActivityAsync(ActivityNode? activityNode, ActivityExecutionContext? owner = null, ScheduleWorkOptions? options = null)
    {
        if (this.GetIsBackgroundExecution())
        {
            // Validate that the specified activity is part of the workflow.
            if (activityNode != null && !WorkflowExecutionContext.NodeActivityLookup.ContainsKey(activityNode.Activity))
                throw new InvalidOperationException("The specified activity is not part of the workflow.");

            var scheduledActivity = new ScheduledActivity
            {
                ActivityNodeId = activityNode?.NodeId,
                OwnerActivityInstanceId = owner?.Id,
                Options = options != null
                    ? new ScheduledActivityOptions
                    {
                        CompletionCallback = options?.CompletionCallback?.Method.Name,
                        Tag = options?.Tag,
                        ExistingActivityInstanceId = options?.ExistingActivityExecutionContext?.Id,
                        PreventDuplicateScheduling = options?.PreventDuplicateScheduling ?? false,
                        Variables = options?.Variables?.ToList(),
                        Input = options?.Input
                    }
                    : null
            };

            var scheduledActivities = this.GetBackgroundScheduledActivities().ToList();
            scheduledActivities.Add(scheduledActivity);
            this.SetBackgroundScheduledActivities(scheduledActivities);
            return;
        }

        var completionCallback = options?.CompletionCallback;
        owner ??= this;

        if (activityNode == null)
        {
            if (completionCallback != null)
            {
                Tag = options?.Tag;
                var completedContext = new ActivityCompletedContext(this, this);
                await completionCallback(completedContext);
            }
            else
                await owner.CompleteActivityAsync();

            return;
        }

        WorkflowExecutionContext.Schedule(activityNode, owner, options);
    }

    /// <summary>
    /// Schedules the specified activities to be executed.
    /// </summary>
    /// <param name="activities">The activities to schedule.</param>
    public async ValueTask ScheduleActivitiesAsync(params IActivity?[] activities) => await ScheduleActivities(activities);

    /// <summary>
    /// Schedules the specified activities to be executed.
    /// </summary>
    /// <param name="activities">The activities to schedule.</param>
    /// <param name="completionCallback">The callback to invoke when the activities complete.</param>
    /// <param name="tag">An optional tag to associate with the activity execution.</param>
    /// <param name="variables">An optional list of variables to declare with the activity execution.</param>
    public ValueTask ScheduleActivities(IEnumerable<IActivity?> activities, ActivityCompletionCallback? completionCallback, object? tag = null, IEnumerable<Variable>? variables = null)
    {
        var options = new ScheduleWorkOptions
        {
            CompletionCallback = completionCallback,
            Tag = tag,
            Variables = variables?.ToList()
        };
        return ScheduleActivities(activities, options);
    }

    /// <summary>
    /// Schedules the specified activities to be executed.
    /// </summary>
    /// <param name="activities">The activities to schedule.</param>
    /// <param name="options">The options used to schedule the activities.</param>
    public async ValueTask ScheduleActivities(IEnumerable<IActivity?> activities, ScheduleWorkOptions? options = null)
    {
        foreach (var activity in activities)
            await ScheduleActivityAsync(activity, options);
    }

    /// <summary>
    /// Creates a bookmark for each payload.
    /// </summary>
    /// <param name="payloads">The payloads to create bookmarks for.</param>
    /// <param name="callback">An optional callback that is invoked when the bookmark is resumed.</param>
    /// <param name="includeActivityInstanceId">Whether or not the activity instance ID should be included in the bookmark payload.</param>
    public void CreateBookmarks(IEnumerable<object> payloads, ExecuteActivityDelegate? callback = null, bool includeActivityInstanceId = true)
    {
        foreach (var payload in payloads)
            CreateBookmark(new CreateBookmarkArgs
            {
                Stimulus = payload,
                Callback = callback,
                IncludeActivityInstanceId = includeActivityInstanceId
            });
    }

    /// <summary>
    /// Adds each bookmark to the list of bookmarks.
    /// </summary>
    /// <param name="bookmarks">The bookmarks to add.</param>
    public void AddBookmarks(IEnumerable<Bookmark> bookmarks)
    {
        _bookmarks.AddRange(bookmarks);
        Taint();
    }

    /// <summary>
    /// Adds a bookmark to the list of bookmarks.
    /// </summary>
    /// <param name="bookmark">The bookmark to add.</param>
    public void AddBookmark(Bookmark bookmark)
    {
        _bookmarks.Add(bookmark);
        Taint();
    }

    /// <summary>
    /// Creates a bookmark so that this activity can be resumed at a later time.
    /// </summary>
    /// <param name="callback">An optional callback that is invoked when the bookmark is resumed.</param>
    /// <param name="metadata">Custom properties to associate with the bookmark.</param>
    /// <returns>The created bookmark.</returns>
    public Bookmark CreateBookmark(ExecuteActivityDelegate callback, IDictionary<string, string>? metadata = null)
    {
        return CreateBookmark(new CreateBookmarkArgs
        {
            Callback = callback,
            Metadata = metadata
        });
    }

    /// <summary>
    /// Creates a bookmark so that this activity can be resumed at a later time.
    /// </summary>
    /// <param name="stimulus">The payload to associate with the bookmark.</param>
    /// <param name="callback">An optional callback that is invoked when the bookmark is resumed.</param>
    /// <param name="includeActivityInstanceId">Whether or not the activity instance ID should be included in the bookmark payload.</param>
    /// <param name="customProperties">Custom properties to associate with the bookmark.</param>
    /// <returns>The created bookmark.</returns>
    public Bookmark CreateBookmark(object stimulus, ExecuteActivityDelegate callback, bool includeActivityInstanceId = true, IDictionary<string, string>? customProperties = null)
    {
        return CreateBookmark(new CreateBookmarkArgs
        {
            Stimulus = stimulus,
            Callback = callback,
            IncludeActivityInstanceId = includeActivityInstanceId,
            Metadata = customProperties
        });
    }

    /// <summary>
    /// Creates a bookmark for the current activity execution context.
    /// </summary>
    /// <param name="stimulus">The payload to associate with the bookmark.</param>
    /// <param name="includeActivityInstanceId">Specifies whether to include the activity instance ID in the bookmark information. Defaults to true.</param>
    /// <param name="customProperties">Additional custom properties to associate with the bookmark. Defaults to null.</param>
    /// <returns>The created bookmark.</returns>
    public Bookmark CreateBookmark(object stimulus, bool includeActivityInstanceId, IDictionary<string, string>? customProperties = null)
    {
        return CreateBookmark(new CreateBookmarkArgs
        {
            Stimulus = stimulus,
            IncludeActivityInstanceId = includeActivityInstanceId,
            Metadata = customProperties
        });
    }

    /// <summary>
    /// Creates a bookmark so that this activity can be resumed at a later time. 
    /// </summary>
    /// <param name="stimulus">The payload to associate with the bookmark.</param>
    /// <param name="metadata">Custom properties to associate with the bookmark.</param>
    /// <returns>The created bookmark.</returns>
    public Bookmark CreateBookmark(object stimulus, IDictionary<string, string>? metadata = null)
    {
        return CreateBookmark(new()
        {
            Stimulus = stimulus,
            Metadata = metadata
        });
    }

    /// <summary>
    /// Creates a bookmark so that this activity can be resumed at a later time.
    /// Creating a bookmark will automatically suspend the workflow after all pending activities have executed.
    /// </summary>
    public Bookmark CreateBookmark(CreateBookmarkArgs? options = null)
    {
        var payload = options?.Stimulus;
        var callback = options?.Callback;
        var bookmarkName = options?.BookmarkName ?? Activity.Type;
        var bookmarkHasher = GetRequiredService<IStimulusHasher>();
        var identityGenerator = GetRequiredService<IIdentityGenerator>();
        var includeActivityInstanceId = options?.IncludeActivityInstanceId ?? true;
        var hash = bookmarkHasher.Hash(bookmarkName, payload, includeActivityInstanceId ? Id : null);
        var bookmarkId = options?.BookmarkId ?? identityGenerator.GenerateId();

        var bookmark = new Bookmark(
            bookmarkId,
            bookmarkName,
            hash,
            payload,
            Activity.Id,
            ActivityNode.NodeId,
            Id,
            _systemClock.UtcNow,
            options?.AutoBurn ?? true,
            callback?.Method.Name,
            options?.AutoComplete ?? true,
            options?.Metadata);

        AddBookmark(bookmark);
        return bookmark;
    }

    /// <summary>
    /// Clear all bookmarks.
    /// </summary>
    public void ClearBookmarks()
    {
        _bookmarks.Clear();
        Taint();
    }

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

    /// <summary>
    /// Resolves a required service using the service provider.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The resolved service.</returns>
    public T GetRequiredService<T>() where T : notnull => WorkflowExecutionContext.GetRequiredService<T>();

    /// <summary>
    /// Resolves a required service using the service provider.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <returns>The resolved service.</returns>
    public object GetRequiredService(Type serviceType) => WorkflowExecutionContext.GetRequiredService(serviceType);

    /// <summary>
    /// Resolves a service using the service provider. If not found, a new instance is created.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The resolved service.</returns>
    public T GetOrCreateService<T>() where T : notnull => WorkflowExecutionContext.GetOrCreateService<T>();

    /// <summary>
    /// Resolves a service using the service provider. If not found, a new instance is created.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <returns>The resolved service.</returns>
    public object GetOrCreateService(Type serviceType) => WorkflowExecutionContext.GetOrCreateService(serviceType);

    /// <summary>
    /// Resolves a service using the service provider.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The resolved service.</returns>
    public T? GetService<T>() where T : notnull => WorkflowExecutionContext.GetService<T>();

    /// <summary>
    /// Resolves all services of the specified type using the service provider.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The resolved services.</returns>
    public IEnumerable<T> GetServices<T>() where T : notnull => WorkflowExecutionContext.GetServices<T>();

    /// <summary>
    /// Resolves a service using the service provider.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <returns>The resolved service.</returns>
    public object? GetService(Type serviceType) => WorkflowExecutionContext.GetService(serviceType);

    /// <summary>
    /// Gets the value of the specified input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <typeparam name="T">The type of the input.</typeparam>
    /// <returns>The input value.</returns>
    public T? Get<T>(Input<T>? input) => input == null ? default : Get<T>(input.MemoryBlockReference());

    /// <summary>
    /// Gets the value of the specified output.
    /// </summary>
    /// <param name="output">The output.</param>
    /// <typeparam name="T">The type of the output.</typeparam>
    /// <returns>The output value.</returns>
    public T? Get<T>(Output<T>? output) => output == null ? default : Get<T>(output.MemoryBlockReference());

    /// <summary>
    /// Gets the value of the specified output.
    /// </summary>
    /// <param name="output">The output.</param>
    /// <returns>The output value.</returns>
    public object? Get(Output? output) => output == null ? null : Get(output.MemoryBlockReference());

    /// <summary>
    /// Gets the value of the specified memory block.
    /// </summary>
    /// <param name="blockReference">The memory block reference.</param>
    /// <returns>The memory block value.</returns>
    /// <exception cref="InvalidOperationException">The memory block does not exist.</exception>
    public object? Get(MemoryBlockReference blockReference)
    {
        return !TryGet(blockReference, out var value)
            ? throw new InvalidOperationException($"The memory block '{blockReference}' does not exist.")
            : value;
    }

    /// <summary>
    /// Gets the value of the specified memory block.
    /// </summary>
    /// <param name="blockReference">The memory block reference.</param>
    /// <typeparam name="T">The type of the memory block.</typeparam>
    /// <returns>The memory block value.</returns>
    public T? Get<T>(MemoryBlockReference blockReference)
    {
        var value = Get(blockReference);
        return value != null ? value.ConvertTo<T>() : default;
    }

    /// <summary>
    /// Tries to get the value of the specified memory block.
    /// </summary>
    /// <param name="blockReference">The memory block reference.</param>
    /// <param name="value">The memory block value.</param>
    /// <returns>True if the memory block exists, false otherwise.</returns>
    public bool TryGet(MemoryBlockReference blockReference, out object? value)
    {
        var memoryBlock = GetMemoryBlock(blockReference);

        if (memoryBlock != null)
        {
            value = memoryBlock.Value;
            return true;
        }

        if (blockReference is Literal literal)
        {
            value = literal.Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Sets a value at the specified memory block.
    /// </summary>
    /// <param name="blockReference">The memory block reference.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="configure">An optional callback that can be used to configure the memory block.</param>
    public void Set(MemoryBlockReference blockReference, object? value, Action<MemoryBlock>? configure = null) => ExpressionExecutionContext.Set(blockReference, value, configure);

    /// <summary>
    /// Sets a value at the specified output.
    /// </summary>
    /// <param name="output">The output.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="outputName">The name of the output.</param>
    /// <typeparam name="T">The type of the output.</typeparam>
    public void Set<T>(Output<T>? output, T? value, [CallerArgumentExpression("output")] string? outputName = null) => Set((Output?)output, value, outputName);

    /// <summary>
    /// Sets a value at the specified output.
    /// </summary>
    /// <param name="output">The output.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="outputName">The name of the output.</param>
    public void Set(Output? output, object? value, [CallerArgumentExpression("output")] string? outputName = null)
    {
        // Store the value in the expression execution memory block.
        ExpressionExecutionContext.Set(output, value);

        // Also store the value in the workflow execution transient activity output register.
        WorkflowExecutionContext.RecordActivityOutput(this, outputName, value);
    }

    /// <summary>
    /// Removes all completion callbacks for the current activity.
    /// </summary>
    public void ClearCompletionCallbacks()
    {
        var entriesToRemove = WorkflowExecutionContext.CompletionCallbacks.Where(x => x.Owner == this).ToList();
        WorkflowExecutionContext.RemoveCompletionCallbacks(entriesToRemove);
    }

    public void Taint()
    {
        if (!IsDirty)
            IsDirty = true;
    }

    public void ClearTaint()
    {
        if (IsDirty)
            IsDirty = false;
    }

    internal void IncrementExecutionCount()
    {
        _executionCount++;
    }

    private MemoryBlock? GetMemoryBlock(MemoryBlockReference locationBlockReference)
    {
        return ExpressionExecutionContext.TryGetBlock(locationBlockReference, out var memoryBlock) ? memoryBlock : null;
    }

    void IDisposable.Dispose()
    {
    }
}