using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core;

public abstract class Behavior : IBehavior
{
    private readonly ICollection<SignalHandlerRegistration> _signalHandlers = new List<SignalHandlerRegistration>();

    public Behavior(IActivity owner)
    {
        Owner = owner;
    }

    public IActivity Owner { get; }
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
    
    protected virtual ValueTask OnSignalReceivedAsync(object signal, SignalContext context)
    {
        OnSignalReceived(signal, context);
        return ValueTask.CompletedTask;
    }

    protected virtual void OnSignalReceived(object signal, SignalContext context)
    {
    }

    protected virtual ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        Execute(context);
        return ValueTask.CompletedTask;
    }

    protected virtual void Execute(ActivityExecutionContext context)
    {
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
    }

    async ValueTask IBehavior.ExecuteAsync(ActivityExecutionContext context) => await ExecuteAsync(context);
}