using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.ComponentTests.Activities;

public class TriggerSignal : CodeActivity
{
    /// <inheritdoc />
    [JsonConstructor]
    private TriggerSignal(string? source = default, int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public TriggerSignal(string eventName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(new Literal<string>(eventName), source, line)
    {
    }

    /// <inheritdoc />
    public TriggerSignal(Func<string> eventName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(Expression.DelegateExpression(eventName), source, line)
    {
    }

    /// <inheritdoc />
    public TriggerSignal(Func<ExpressionExecutionContext, string?> eventName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(Expression.DelegateExpression(eventName), source, line)
    {
    }

    /// <inheritdoc />
    public TriggerSignal(Variable<string> variable, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) => EventName = new Input<string>(variable);

    /// <inheritdoc />
    public TriggerSignal(Literal<string> literal, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) => EventName = new Input<string>(literal);

    /// <inheritdoc />
    public TriggerSignal(Expression expression, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) => EventName = new Input<string>(expression, new MemoryBlockReference());

    /// <inheritdoc />
    public TriggerSignal(Input<string> eventName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) => EventName = eventName;

    public Input<string> EventName { get; set; }

    protected override void Execute(ActivityExecutionContext context)
    {
        var testEventManager = context.GetRequiredService<ISignalManager>();
        var eventName = EventName.Get(context);
        testEventManager.Trigger(eventName);
    }
}