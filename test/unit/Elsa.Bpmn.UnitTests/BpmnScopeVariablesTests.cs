using System.Text.Json;
using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Hosting;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Memory;
using System.Threading.Tasks;

namespace Elsa.Bpmn.UnitTests;

/// <summary>
/// The one callback the interpreter makes into the host, and the three answers it distinguishes. The third —
/// "I have it and cannot give it to you" — is the one worth having: reported as null or absent instead, a
/// collection-mode multi-instance resolves to zero instances and the process completes as though there had been
/// nothing to do.
/// </summary>
public class BpmnScopeVariablesTests
{
    [Test]
    [DisplayName("A variable nothing in scope declares reads as absent")]
    public async Task TryRead_ReturnsFalse_ForAnUndeclaredVariable()
    {
        var variables = await ReaderForAsync(new Variable<string>("declared", "value"));

        await Assert.That(variables.TryRead("undeclared", out var value)).IsFalse();
        await Assert.That(value.Presence).IsEqualTo(BpmnValuePresence.Absent);
    }

    [Test]
    [DisplayName("A declared variable holding null reads as present-and-null, not as absent")]
    public async Task TryRead_ReturnsNull_ForADeclaredVariableHoldingNull()
    {
        var variables = await ReaderForAsync(new Variable<string?>("empty", null));

        await Assert.That(variables.TryRead("empty", out var value)).IsTrue();
        await Assert.That(value.Presence).IsEqualTo(BpmnValuePresence.Null);
    }

    [Test]
    [DisplayName("A declared variable holding a value reads as present, inline")]
    public async Task TryRead_ReturnsThePayload_ForADeclaredVariableHoldingAValue()
    {
        var variables = await ReaderForAsync(new Variable<string[]>("items", ["alpha", "beta"]));

        await Assert.That(variables.TryRead("items", out var value)).IsTrue();
        await Assert.That(value.Presence).IsEqualTo(BpmnValuePresence.Present);
        await Assert.That(value.HasValue).IsTrue();
        await Assert.That(value.Json!.Value.ValueKind).IsEqualTo(JsonValueKind.Array);
        await Assert.That(value.Json.Value.EnumerateArray().Select(item => item.GetString())).IsEquivalentTo(new string?[] { "alpha", "beta" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("An integer carries the type hint the interpreter understands")]
    public async Task TryRead_HintsInteger_ForAWholeNumber()
    {
        var variables = await ReaderForAsync(new Variable<int>("count", 3));

        await Assert.That(variables.TryRead("count", out var value)).IsTrue();
        await Assert.That(value.TypeHint).IsEqualTo(BpmnValueTypes.Integer);
    }

    [Test]
    [DisplayName("A value that cannot cross the port inline reads as stored externally, not as null")]
    public async Task TryRead_ReturnsStoredExternally_ForAValueJsonCannotCarry()
    {
        // A cyclic object graph is the everyday version of this: a node holding its parent. The variable is neither
        // missing nor null, and saying either would be a quiet wrong answer — the interpreter treats
        // StoredExternally as unreadable and faults, naming the element that asked.
        var cyclic = new SelfReferencing();
        cyclic.Self = cyclic;

        var variables = await ReaderForAsync(new Variable<SelfReferencing>("cyclic", cyclic));

        await Assert.That(variables.TryRead("cyclic", out var value)).IsTrue();
        await Assert.That(value.Presence).IsEqualTo(BpmnValuePresence.StoredExternally);
        await Assert.That(value.HasValue).IsFalse();
    }

    [Test]
    [DisplayName("A value bare JsonSerializerDefaults cannot serialize, but Elsa's configured serializer can, reads as present")]
    public async Task TryRead_ReturnsThePayload_ForAValueOnlyElsasSerializerCanCarry()
    {
        // System.Text.Json refuses to serialize a System.Type instance under bare defaults — it throws
        // NotSupportedException. Elsa's configured serializer carries it via TypeJsonConverter. A reader using bare
        // defaults collapses this to StoredExternally even though Elsa can hand the value over intact.
        var variables = await ReaderForAsync(new Variable<Type>("clrType", typeof(string)));

        await Assert.That(variables.TryRead("clrType", out var value)).IsTrue();
        await Assert.That(value.Presence).IsEqualTo(BpmnValuePresence.Present);
        await Assert.That(value.HasValue).IsTrue();
    }

    [Test]
    [DisplayName("A variable declared by an enclosing scope is visible to the scope inside it")]
    public async Task TryRead_WalksOutward_ForAVariableOfAnEnclosingScope()
    {
        // BPMN data scoping and Elsa's agree: an inner scope sees the enclosing scope's variables.
        var outerVariable = new Variable<string>("outer", "value");
        var inner = new BpmnProcess { Id = "inner" };
        var outer = new BpmnProcess { Id = "outer", Variables = { outerVariable }, Activities = { inner } };

        var outerContext = await new ActivityTestFixture(outer).BuildAsync();
        outerContext.ExpressionExecutionContext.Memory.Declare(outer.Variables);

        var workflowExecutionContext = outerContext.WorkflowExecutionContext;
        await workflowExecutionContext.ActivityRegistry.RegisterAsync(typeof(BpmnProcess));
        var innerContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(inner, new() { Owner = outerContext });

        await Assert.That(new BpmnScopeVariables(innerContext).TryRead("outer", out var value)).IsTrue();
        await Assert.That(value.Presence).IsEqualTo(BpmnValuePresence.Present);
    }

    /// <summary>
    /// A reader over a scope declaring the given variables, exactly as <c>Container.ExecuteAsync</c> declares them
    /// before scheduling anything.
    /// </summary>
    private static async Task<BpmnScopeVariables> ReaderForAsync(params Variable[] variables)
    {
        var process = new BpmnProcess { Id = "scope" };

        foreach (var variable in variables)
            process.Variables.Add(variable);

        var context = await new ActivityTestFixture(process).BuildAsync();
        context.ExpressionExecutionContext.Memory.Declare(process.Variables);

        return new(context);
    }

    private sealed class SelfReferencing
    {
        public SelfReferencing? Self { get; set; }
    }
}
