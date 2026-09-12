using System.Text.Json;
using Bpmn.Model;
using Bpmn.Semantics;
using Elsa.Extensions;
using Elsa.Workflows;

namespace Elsa.Bpmn.Hosting;

/// <summary>
/// Reads a BPMN scope's container-scoped variables for the interpreter — the one callback the port makes.
/// </summary>
/// <remarks>
/// <para>
/// The read is three-valued, and the three answers are not interchangeable. <c>false</c> says there is no such
/// variable, and faults the element that asked. <see cref="BpmnValuePresence.Null"/> says the variable exists and
/// holds nothing, which a collection-mode multi-instance resolves as zero instances. <see
/// cref="BpmnValuePresence.StoredExternally"/> says the host has a value it cannot put on the wire, and faults
/// rather than reading as empty. Collapsing any of them into another is exactly the quiet wrong answer the port's
/// three-valued read exists to prevent.
/// </para>
/// <para>
/// This host reaches <see cref="BpmnValuePresence.StoredExternally"/> by one route: a value JSON cannot carry. It
/// deliberately does <b>not</b> report a driver-backed variable that way. <c>PersistentVariablesMiddleware</c> calls
/// <c>IVariablePersistenceManager.LoadVariablesAsync</c> before the pipeline runs and passes no <c>excludeTags</c>,
/// so a driver-backed value is already materialized into its memory block by the time any scope evaluates. Were a
/// host to exclude a tag, the skipped variable would be undetectable from here: <c>VariablePersistenceManager</c>
/// marks the block <c>IsInitialized</c> <i>before</i> testing the exclusion, so a variable whose driver was never
/// read is indistinguishable from one whose driver returned null, and nothing else records the exclusion. Guessing
/// between them would either fault materialized variables or quietly report unread ones as null; answering only
/// what the block actually says is the honest option available without changing <c>Elsa.Workflows.Core</c>.
/// </para>
/// <para>
/// Synchronous, which the port requires and Elsa can honour for the same reason: nothing here needs to await a
/// driver, because the middleware already did.
/// </para>
/// </remarks>
internal sealed class BpmnScopeVariables(ActivityExecutionContext context) : IBpmnVariableReader
{
    /// <summary>A value this host holds but cannot represent inline.</summary>
    private static readonly BpmnValue Unrepresentable = new(BpmnValuePresence.StoredExternally, BpmnValueTypes.Any, null);

    // Resolved once per snapshot, not per TryRead: GetOptions() rebuilds the converter list on every call, and the
    // interpreter calls this port once per variable it needs.
    private readonly Lazy<JsonSerializerOptions> _serializerOptions = new(() => context.GetRequiredService<IPayloadSerializer>().GetOptions());

    /// <inheritdoc />
    public bool TryRead(string name, out BpmnValue value)
    {
        var expressionExecutionContext = context.ExpressionExecutionContext;

        // Resolution walks outward from this scope, which is both Elsa's variable scoping and BPMN's: an inner scope
        // sees the enclosing scope's data. A name nothing in scope declares is genuinely absent, and saying so is what
        // turns a mistyped collection variable into a fault naming the element instead of a loop that never runs.
        if (expressionExecutionContext.GetVariable(name) is not { } variable || !expressionExecutionContext.TryGetBlock(variable, out var block))
        {
            value = BpmnValue.Absent;
            return false;
        }

        value = Represent(block.Value);
        return true;
    }

    /// <summary>
    /// The interpreter's view of a CLR value. Values cross the port as JSON with a host-interpreted type hint, so
    /// anything Elsa can hold that JSON cannot carry is reported as held outside the payload rather than as absent
    /// or null.
    /// </summary>
    private BpmnValue Represent(object? value)
    {
        if (value is null)
            return BpmnValue.Null;

        try
        {
            // Serialized against the value's own runtime type, not against the declared `object`: the declared-type
            // overload is what routes through PolymorphicObjectConverterFactory, which wraps a boxed array or scalar
            // in Elsa's own type-tagged envelope — exactly the shape the interpreter, reading plain JSON on its own
            // wire format, does not expect. Serializing against the runtime type keeps a plain value plain while
            // still picking up every other converter Elsa registers (TypeJsonConverter, VariableConverterFactory,
            // enum-as-string, and so on), and still reaches PolymorphicObjectConverterFactory for a value whose
            // runtime type genuinely is one it claims (ExpandoObject, Dictionary<string, object>).
            return BpmnValue.From(JsonSerializer.SerializeToElement(value, value.GetType(), _serializerOptions.Value), TypeHintOf(value));
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            // The variable is not missing and not null: this host has it, and cannot hand it over. A cyclic object
            // graph and a type with no converter both land here. Reporting it as null or absent would resolve a
            // collection-mode loop to zero instances and complete as though there had been nothing to do.
            return Unrepresentable;
        }
    }

    /// <summary>
    /// The type hint that travels with the value. The interpreter understands two — <see cref="BpmnValueTypes.Integer"/>,
    /// which multi-instance cardinality and loop indices use, and <see cref="BpmnValueTypes.Any"/> — and assigns no
    /// meaning to any other.
    /// </summary>
    private static string TypeHintOf(object value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong
            ? BpmnValueTypes.Integer
            : BpmnValueTypes.Any;
}
