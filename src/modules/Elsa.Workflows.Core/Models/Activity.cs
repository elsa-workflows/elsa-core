using System.Diagnostics;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Core.Behaviors;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Base class for custom activities.
/// </summary>
[DebuggerDisplay("{Type} - {Id}")]
public abstract class Activity : IActivity, ISignalHandler
{
    private readonly ICollection<SignalHandlerRegistration> _signalHandlers = new List<SignalHandlerRegistration>();

    /// <summary>
    /// Constructor.
    /// </summary>
    protected Activity(string? source = default, int? line = default)
    {
        Source = source;
        Line = line;
        Type = ActivityTypeNameHelper.GenerateTypeName(GetType());
        Version = 1;
        Behaviors.Add<ExecutionLoggingBehavior>(this);
        Behaviors.Add<ScheduledChildCallbackBehavior>(this);
    }

    /// <inheritdoc />
    protected Activity(string activityType, int version = 1, string? source = default, int? line = default) : this(source, line)
    {
        Type = activityType;
        Version = version;
    }

    /// <summary>
    /// The unique ID of this activity within the <see cref="Workflow"/>.
    /// </summary>
    public string Id { get; set; } = default!;
    
    /// <summary>
    /// The technical type name.
    /// </summary>
    public string Type { get; set; }
    
    /// <summary>
    /// The version number.
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// A flag indicating whether this activity can be used for starting a workflow.
    /// Usually used for triggers, but also used to disambiguate between two or more starting activities and no starting activity was specified.
    /// </summary>
    public bool CanStartWorkflow { get; set; }
    
    
    /// <summary>
    /// A flag indicating if this activity should execute synchronously or asynchronously.
    /// By default, activities with an <see cref="ActivityKind"/> of <see cref="ActivityKind.Action"/>, <see cref="ActivityKind.Task"/> or <see cref="ActivityKind.Trigger"/>
    /// will execute synchronously, while activities of the <see cref="ActivityKind.Job"/> kind will execute asynchronously.
    /// </summary>
    public bool RunAsynchronously { get; set; }
    
    /// <summary>
    /// A bag of properties that can be used by custom activities and other code such as middleware components to store additional values with the activity.
    /// </summary>
    public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
    
    /// <summary>
    /// Automatically set to the current source file name when instantiating this activity inside of a workflow class or composite activity class.
    /// </summary>
    public string? Source { get; set; }
    
    /// <summary>
    /// Automatically set to the current line of code when instantiating this activity inside of a workflow class or composite activity class.
    /// </summary>
    public int? Line { get; set; }
    
    /// <summary>
    /// Stores metadata such as x and y coordinates when created via the designer.
    /// </summary>
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// A collection of reusable behaviors to add to this activity.
    /// </summary>
    [JsonIgnore]
    public ICollection<IBehavior> Behaviors { get; } = new List<IBehavior>();

    /// <summary>
    /// Override this method to implement activity-specific logic.
    /// </summary>
    protected virtual ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        Execute(context);
        return ValueTask.CompletedTask;
    }
    
    /// <summary>
    /// Override this method to implement activity-specific logic.
    /// </summary>
    protected virtual void Execute(ActivityExecutionContext context)
    {
    }

    /// <summary>
    /// Override this method to handle any signals sent from downstream activities.
    /// </summary>
    protected virtual ValueTask OnSignalReceivedAsync(object signal, SignalContext context)
    {
        OnSignalReceived(signal, context);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Override this method to handle any signals sent from downstream activities.
    /// </summary>
    protected virtual void OnSignalReceived(object signal, SignalContext context)
    {
    }

    /// <summary>
    /// Register a signal handler delegate.
    /// </summary>
    protected void OnSignalReceived(Type signalType, Func<object, SignalContext, ValueTask> handler) => _signalHandlers.Add(new SignalHandlerRegistration(signalType, handler));
    
    /// <summary>
    /// Register a signal handler delegate.
    /// </summary>
    protected void OnSignalReceived<T>(Func<T, SignalContext, ValueTask> handler) => OnSignalReceived(typeof(T), (signal, context) => handler((T)signal, context));

    /// <summary>
    /// Register a signal handler delegate.
    /// </summary>
    protected void OnSignalReceived<T>(Action<T, SignalContext> handler)
    {
        OnSignalReceived<T>((signal, context) =>
        {
            handler(signal, context);
            return ValueTask.CompletedTask;
        });
    }
    
    /// <summary>
    /// Notify the workflow that this activity completed.
    /// </summary>
    protected async ValueTask CompleteAsync(ActivityExecutionContext context)
    {
        await context.CompleteActivityAsync();
    }

    async ValueTask IActivity.ExecuteAsync(ActivityExecutionContext context)
    {
        await ExecuteAsync(context);

        // Invoke behaviors.
        foreach (var behavior in Behaviors) await behavior.ExecuteAsync(context);
    }

    async ValueTask ISignalHandler.HandleSignalAsync(object signal, SignalContext context)
    {
        // Give derived activity a chance to do something with the signal.
        await OnSignalReceivedAsync(signal, context);

        // Invoke registered signal delegates for this particular type of signal.
        var signalType = signal.GetType();
        var handlers = _signalHandlers.Where(x => x.SignalType == signalType);

        foreach (var registration in handlers)
            await registration.Handler(signal, context);

        // Invoke behaviors.
        foreach (var behavior in Behaviors) await behavior.HandleSignalAsync(signal, context);
    }
}