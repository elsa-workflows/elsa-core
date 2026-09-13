using Elsa.Testing.Shared;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ParentChildInputs;

/// <summary>
/// Tests for the flowchart completion feature using various workflow setups.
/// </summary>
public class Tests : IAsyncDisposable
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).WithCapturingTextWriter(_capturingTextWriter).Build();
    }

    [Test]
    [DisplayName("Child activity receives workflow input from parent even if same name.")]
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import child workflow.
        var childFileName = $"Scenarios/ParentChildInputs/Workflows/child.json";
        await _services.ImportWorkflowDefinitionAsync(childFileName);

        // Import parent workflow.
        var parentFileName = $"Scenarios/ParentChildInputs/Workflows/parent1.json";
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync(parentFileName);

        // Execute.
        var input = new Dictionary<string, object> { ["Input1"] = "Foo" };
        await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId, input);

        // Assert expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(new[] { "Parent: Foo", "Child: Bar" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }
    
    [Test]
    [DisplayName("Child activity receives input from parent event if same name and does not use global workflow input.")]
    public async Task Test2()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import child workflow.
        var childFileName = $"Scenarios/ParentChildInputs/Workflows/child.json";
        await _services.ImportWorkflowDefinitionAsync(childFileName);

        // Import parent workflow.
        var parentFileName = $"Scenarios/ParentChildInputs/Workflows/parent2.json";
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync(parentFileName);

        // Execute.
        var input = new Dictionary<string, object> { ["Input1"] = "Foo" };
        await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId, input);

        // Assert expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(new[] { "Parent: Foo", "Child: Foo" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
