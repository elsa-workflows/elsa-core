using System.Text.Json;
using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Hosting;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Memory;

namespace Elsa.Bpmn.UnitTests;

/// <summary>
/// The one callback the interpreter makes into the host, and the three answers it distinguishes. The third —
/// "I have it and cannot give it to you" — is the one worth having: reported as null or absent instead, a
/// collection-mode multi-instance resolves to zero instances and the process completes as though there had been
/// nothing to do.
/// </summary>
public class BpmnScopeVariablesTests
{
    [Fact(DisplayName = "A variable nothing in scope declares reads as absent")]
    public async Task TryRead_ReturnsFalse_ForAnUndeclaredVariable()
    {
        var variables = await ReaderForAsync(new Variable<string>("declared", "value"));

        Assert.False(variables.TryRead("undeclared", out var value));
        Assert.Equal(BpmnValuePresence.Absent, value.Presence);
    }

    [Fact(DisplayName = "A declared variable holding null reads as present-and-null, not as absent")]
    public async Task TryRead_ReturnsNull_ForADeclaredVariableHoldingNull()
    {
        var variables = await ReaderForAsync(new Variable<string?>("empty", null));

        Assert.True(variables.TryRead("empty", out var value));
        Assert.Equal(BpmnValuePresence.Null, value.Presence);
    }

    [Fact(DisplayName = "A declared variable holding a value reads as present, inline")]
    public async Task TryRead_ReturnsThePayload_ForADeclaredVariableHoldingAValue()
    {
        var variables = await ReaderForAsync(new Variable<string[]>("items", ["alpha", "beta"]));

        Assert.True(variables.TryRead("items", out var value));
        Assert.Equal(BpmnValuePresence.Present, value.Presence);
        Assert.True(value.HasValue);
        Assert.Equal(JsonValueKind.Array, value.Json!.Value.ValueKind);
        Assert.Equal(["alpha", "beta"], value.Json.Value.EnumerateArray().Select(item => item.GetString()));
    }

    [Fact(DisplayName = "An integer carries the type hint the interpreter understands")]
    public async Task TryRead_HintsInteger_ForAWholeNumber()
    {
        var variables = await ReaderForAsync(new Variable<int>("count", 3));

        Assert.True(variables.TryRead("count", out var value));
        Assert.Equal(BpmnValueTypes.Integer, value.TypeHint);
    }

    [Fact(DisplayName = "A value that cannot cross the port inline reads as stored externally, not as null")]
    public async Task TryRead_ReturnsStoredExternally_ForAValueJsonCannotCarry()
    {
        // A cyclic object graph is the everyday version of this: a node holding its parent. The variable is neither
        // missing nor null, and saying either would be a quiet wrong answer — the interpreter treats
        // StoredExternally as unreadable and faults, naming the element that asked.
        var cyclic = new SelfReferencing();
        cyclic.Self = cyclic;

        var variables = await ReaderForAsync(new Variable<SelfReferencing>("cyclic", cyclic));

        Assert.True(variables.TryRead("cyclic", out var value));
        Assert.Equal(BpmnValuePresence.StoredExternally, value.Presence);
        Assert.False(value.HasValue);
    }

    [Fact(DisplayName = "A variable declared by an enclosing scope is visible to the scope inside it")]
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

        Assert.True(new BpmnScopeVariables(innerContext).TryRead("outer", out var value));
        Assert.Equal(BpmnValuePresence.Present, value.Presence);
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
