using System.Diagnostics;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Behaviors;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

[DebuggerDisplay("{Type} - {Id}")]
public abstract class ActivityBase : IActivity, ISignalHandler
{
    private readonly string? _sourceFileName;
    private readonly int? _sourceLineNumber;
    private readonly ICollection<SignalHandlerRegistration> _signalHandlers = new List<SignalHandlerRegistration>();

    protected ActivityBase(string? source = default, int? line = default)
    {
        _sourceFileName = source;
        _sourceLineNumber = line;
        
        Type = ActivityTypeNameHelper.GenerateTypeName(GetType());
        Version = 1;
        Behaviors.Add<ExecutionLoggingBehavior>(this);
        Behaviors.Add<ScheduledChildCallbackBehavior>(this);
    }

    protected ActivityBase(string activityType, int version = 1, string? source = default, int? line = default) : this(source, line)
    {
        Type = activityType;
        Version = version;
    }

    public string Id { get; set; } = default!;
    public string Type { get; set; }
    public int Version { get; set; }
    public bool CanStartWorkflow { get; set; }
    public bool RunAsynchronously { get; set; }
    public IDictionary<string, object> ApplicationProperties { get; set; } = new Dictionary<string, object>();
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// A collection of reusable behaviors to add to this activity.
    /// </summary>
    [JsonIgnore]
    public ICollection<IBehavior> Behaviors { get; } = new List<IBehavior>();

    protected virtual ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        Execute(context);
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask OnSignalReceivedAsync(object signal, SignalContext context)
    {
        OnSignalReceived(signal, context);
        return ValueTask.CompletedTask;
    }

    protected virtual void OnSignalReceived(object signal, SignalContext context)
    {
    }

    protected virtual void Execute(ActivityExecutionContext context)
    {
    }

    /// <summary>
    /// Notify the system that this activity completed.
    /// </summary>
    protected async ValueTask CompleteAsync(ActivityExecutionContext context)
    {
        await context.CompleteActivityAsync();
    }

    protected void OnSignalReceived(Type signalType, Func<object, SignalContext, ValueTask> handler) => _signalHandlers.Add(new SignalHandlerRegistration(signalType, handler));
    protected void OnSignalReceived<T>(Func<T, SignalContext, ValueTask> handler) => OnSignalReceived(typeof(T), (signal, context) => handler((T)signal, context));

    protected void OnSignalReceived<T>(Action<T, SignalContext> handler)
    {
        OnSignalReceived<T>((signal, context) =>
        {
            handler(signal, context);
            return ValueTask.CompletedTask;
        });
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