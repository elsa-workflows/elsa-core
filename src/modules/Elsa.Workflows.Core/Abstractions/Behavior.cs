using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core;

/// <inheritdoc />
public abstract class Behavior : IBehavior
{
    private readonly ICollection<SignalHandlerRegistration> _signalReceivedHandlers = new List<SignalHandlerRegistration>();
    private readonly ICollection<SignalHandlerRegistration> _signalCapturedHandlers = new List<SignalHandlerRegistration>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Behavior"/> class.
    /// </summary>
    /// <param name="owner">The activity that owns this behavior.</param>
    protected Behavior(IActivity owner)
    {
        Owner = owner;
    }

    /// <summary>
    /// The activity that owns this behavior.
    /// </summary>
    public IActivity Owner { get; }
    
    /// <summary>
    /// Registers a delegate to be invoked when a signal of the specified type is received.
    /// </summary>
    /// <param name="signalType">The type of signal to register a handler for.</param>
    /// <param name="handler">The delegate to invoke when a signal of the specified type is received.</param>
    protected void OnSignalReceived(Type signalType, Func<object, SignalContext, ValueTask> handler) => _signalReceivedHandlers.Add(new SignalHandlerRegistration(signalType, handler));
    
    /// <summary>
    /// Registers a delegate to be invoked when a signal of the specified type is received.
    /// </summary>
    /// <param name="handler">The delegate to invoke when a signal of the specified type is received.</param>
    /// <typeparam name="T">The type of signal to register a handler for.</typeparam>
    protected void OnSignalReceived<T>(Func<T, SignalContext, ValueTask> handler) => OnSignalReceived(typeof(T), (signal, context) => handler((T)signal, context));

    /// <summary>
    /// Registers a delegate to be invoked when a signal of the specified type is received.
    /// </summary>
    /// <param name="handler">The delegate to invoke when a signal of the specified type is received.</param>
    /// <typeparam name="T">The type of signal to register a handler for.</typeparam>
    protected void OnSignalReceived<T>(Action<T, SignalContext> handler)
    {
        OnSignalReceived<T>((signal, context) =>
        {
            handler(signal, context);
            return ValueTask.CompletedTask;
        });
    }
    
    /// <summary>
    /// Registers a delegate to be invoked when a signal of the specified type is received.
    /// </summary>
    /// <param name="signal">The type of signal to register a handler for.</param>
    /// <param name="context">The signal context.</param>
    protected virtual ValueTask OnSignalReceivedAsync(object signal, SignalContext context)
    {
        OnSignalReceived(signal, context);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Registers a delegate to be invoked when a signal of the specified type is received.
    /// </summary>
    /// <param name="signal">The signal to register a handler for.</param>
    /// <param name="context">The signal context.</param>
    protected virtual void OnSignalReceived(object signal, SignalContext context)
    {
    }
    
    /// <summary>
    /// Registers a delegate to be invoked when a signal of the specified type is received.
    /// </summary>
    /// <param name="signalType">The type of signal to register a handler for.</param>
    /// <param name="handler">The delegate to invoke when a signal of the specified type is received.</param>
    protected void OnSignalCaptured(Type signalType, Func<object, SignalContext, ValueTask> handler) => _signalCapturedHandlers.Add(new SignalHandlerRegistration(signalType, handler));
    
    /// <summary>
    /// Registers a delegate to be invoked when a signal of the specified type is received.
    /// </summary>
    /// <param name="handler">The delegate to invoke when a signal of the specified type is received.</param>
    /// <typeparam name="T">The type of signal to register a handler for.</typeparam>
    protected void OnSignalCaptured<T>(Func<T, SignalContext, ValueTask> handler) => OnSignalCaptured(typeof(T), (signal, context) => handler((T)signal, context));

    /// <summary>
    /// Registers a delegate to be invoked when a signal of the specified type is received.
    /// </summary>
    /// <param name="handler">The delegate to invoke when a signal of the specified type is received.</param>
    /// <typeparam name="T">The type of signal to register a handler for.</typeparam>
    protected void OnSignalCaptured<T>(Action<T, SignalContext> handler)
    {
        OnSignalCaptured<T>((signal, context) =>
        {
            handler(signal, context);
            return ValueTask.CompletedTask;
        });
    }
    
    /// <summary>
    /// Registers a delegate to be invoked when a signal of the specified type is received.
    /// </summary>
    /// <param name="signal">The type of signal to register a handler for.</param>
    /// <param name="context">The signal context.</param>
    protected virtual ValueTask OnSignalCapturedAsync(object signal, SignalContext context)
    {
        OnSignalCaptured(signal, context);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Registers a delegate to be invoked when a signal of the specified type is received.
    /// </summary>
    /// <param name="signal">The signal to register a handler for.</param>
    /// <param name="context">The signal context.</param>
    protected virtual void OnSignalCaptured(object signal, SignalContext context)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    protected virtual ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        Execute(context);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Invoked when the activity executes.
    /// </summary>
    /// <param name="context"></param>
    protected virtual void Execute(ActivityExecutionContext context)
    {
    }

    async ValueTask ISignalHandler.CaptureSignalAsync(object signal, SignalContext context)
    {
        // Give derived activity a chance to do something with the signal.
        await OnSignalCapturedAsync(signal, context);

        // Invoke registered signal delegates for this particular type of signal.
        var signalType = signal.GetType();
        var handlers = _signalCapturedHandlers.Where(x => x.SignalType == signalType);

        foreach (var registration in handlers)
            await registration.Handler(signal, context);
    }

    async ValueTask ISignalHandler.ReceiveSignalAsync(object signal, SignalContext context)
    {
        // Give derived activity a chance to do something with the signal.
        await OnSignalReceivedAsync(signal, context);

        // Invoke registered signal delegates for this particular type of signal.
        var signalType = signal.GetType();
        var handlers = _signalReceivedHandlers.Where(x => x.SignalType == signalType);

        foreach (var registration in handlers)
            await registration.Handler(signal, context);
    }

    async ValueTask IBehavior.ExecuteAsync(ActivityExecutionContext context) => await ExecuteAsync(context);
}