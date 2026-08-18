using System.Text.Json;
using Bpmn.Model.State;
using Elsa.Workflows;

namespace Elsa.Bpmn.Hosting;

/// <summary>
/// A BPMN scope's own memory: the interpreter's execution state, and the ledger of work this scope started.
/// </summary>
/// <remarks>
/// Both live in the scope's <see cref="ActivityExecutionContext.Properties"/>, because that is where per-execution
/// state belongs and because the handle-to-context map must survive anything that rewrites
/// <see cref="ActivityExecutionContext.Tag"/>. Both are held as serialized JSON strings rather than as objects:
/// the property bag is written through <c>PolymorphicObjectConverter</c>, which silently drops the type of a value
/// whose type has no registered alias and hands back a loose <c>ExpandoObject</c> on the way in.
/// </remarks>
internal sealed class BpmnScopeMemory
{
    /// <summary>The property key holding the interpreter's execution state.</summary>
    public const string ExecutionStatePropertyKey = "Bpmn:ExecutionState";

    /// <summary>The property key holding this scope's work ledger.</summary>
    public const string WorkLedgerPropertyKey = "Bpmn:WorkLedger";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General);

    private readonly ActivityExecutionContext _context;

    private BpmnScopeMemory(ActivityExecutionContext context, BpmnExecutionState? state, BpmnWorkLedger work)
    {
        _context = context;
        State = state;
        Work = work;
    }

    /// <summary>The state the interpreter last returned, or <c>null</c> before the scope has been started.</summary>
    public BpmnExecutionState? State { get; set; }

    /// <summary>The work this scope has started and not finished.</summary>
    public BpmnWorkLedger Work { get; }

    /// <summary>Reads a scope's memory from its property bag.</summary>
    public static BpmnScopeMemory Load(ActivityExecutionContext context) =>
        new(context, Read<BpmnExecutionState>(context, ExecutionStatePropertyKey), Read<BpmnWorkLedger>(context, WorkLedgerPropertyKey) ?? new BpmnWorkLedger());

    /// <summary>Reads a JSON string property, or returns <c>null</c> when it is absent.</summary>
    public static T? Read<T>(ActivityExecutionContext context, string key) where T : class =>
        context.Properties.TryGetValue(key, out var value) && value is string json && !string.IsNullOrWhiteSpace(json)
            ? JsonSerializer.Deserialize<T>(json, SerializerOptions)
            : null;

    /// <summary>Writes a value as a JSON string property.</summary>
    public static void Write<T>(ActivityExecutionContext context, string key, T value) =>
        context.Properties[key] = JsonSerializer.Serialize(value, SerializerOptions);

    /// <summary>Persists the execution state.</summary>
    public void SaveState()
    {
        if (State is not null)
            Write(_context, ExecutionStatePropertyKey, State);
    }

    /// <summary>Persists the work ledger.</summary>
    public void SaveWork() => Write(_context, WorkLedgerPropertyKey, Work);
}
