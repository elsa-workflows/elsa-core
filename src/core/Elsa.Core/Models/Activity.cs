using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Elsa.Behaviors;
using Elsa.Helpers;
using Elsa.Services;

namespace Elsa.Models;

public abstract class Activity : IActivity, ISignalHandler
{
    private readonly ICollection<SignalHandlerRegistration> _signalHandlers = new List<SignalHandlerRegistration>();

    protected Activity()
    {
        TypeName = ActivityTypeNameHelper.GenerateTypeName(GetType());
        Behaviors.Add<ScheduledChildCallbackBehavior>();
        Behaviors.Add<AutoCompleteBehavior>();
    }

    protected Activity(string activityType) : this()
    {
        TypeName = activityType;
    }

    public string Id { get; set; } = default!;
    public string TypeName { get; set; }
    public bool CanStartWorkflow { get; set; }
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
    /// Notify the sytem that this activity completed.
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

public abstract class ActivityWithResult : Activity
{
    protected ActivityWithResult()
    {
    }

    protected ActivityWithResult(string activityType) : base(activityType)
    {
    }

    protected ActivityWithResult(RegisterLocationReference? outputTarget)
    {
        if (outputTarget != null) this.CaptureOutput(outputTarget);
    }

    public Output Result { get; } = new();
}

public abstract class Activity<T> : ActivityWithResult
{
    protected Activity()
    {
    }

    protected Activity(string activityType) : base(activityType)
    {
    }

    protected Activity(RegisterLocationReference? outputTarget) : base(outputTarget)
    {
    }
}

public static class ActivityWithResultExtensions
{
    public static T CaptureOutput<T>(this T activity, Expression<Func<T, Output>> propertyExpression, RegisterLocationReference locationReference) where T : IActivity
    {
        var output = activity.GetPropertyValue(propertyExpression)!;
        output.Targets.Add(locationReference);
        return activity;
    }

    public static T CaptureOutput<T>(this T activity, RegisterLocationReference locationReference) where T : ActivityWithResult => activity.CaptureOutput(x => x.Result, locationReference);
}

internal record SignalHandlerRegistration(Type SignalType, Func<object, SignalContext, ValueTask> Handler);