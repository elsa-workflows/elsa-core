using Elsa.Testing.Shared;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ParentChildOutputMapping;

/// <summary>
/// Tests for mapping an activity's output directly to the workflow's output definition.
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
    [DisplayName("Parent workflow receives output from child workflow.")]
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import child workflow.
        var sumFileName = "Scenarios/ParentChildOutputMapping/Workflows/sum.json";
        await _services.ImportWorkflowDefinitionAsync(sumFileName);

        // Import parent workflow.
        var calculatorFileName = "Scenarios/ParentChildOutputMapping/Workflows/calculator.json";
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync(calculatorFileName);

        // Execute.
        await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);

        // Assert expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(new[] { "Result: 9" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }
    
    [Test]
    [DisplayName("Multiple parent/child workflows receive output from child workflows even when using same output names.")]
    public async Task Test2()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflows.
        await _services.ImportWorkflowDefinitionAsync("Scenarios/ParentChildOutputMapping/Workflows/a.json");
        await _services.ImportWorkflowDefinitionAsync("Scenarios/ParentChildOutputMapping/Workflows/b.json");
        await _services.ImportWorkflowDefinitionAsync("Scenarios/ParentChildOutputMapping/Workflows/c.json");

        // Import root workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync("Scenarios/ParentChildOutputMapping/Workflows/d.json");

        // Execute.
        await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);

        // Assert expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(new[] { "FooBar" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }
    
    [Test]
    [DisplayName("Parent workflow receives output from child workflow when using SetOutput activity.")]
    public async Task Test3()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import child workflow.
        var producerFileName = "Scenarios/ParentChildOutputMapping/Workflows/producer.json";
        await _services.ImportWorkflowDefinitionAsync(producerFileName);

        // Import parent workflow.
        var consumerFileName = "Scenarios/ParentChildOutputMapping/Workflows/consumer.json";
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync(consumerFileName);

        // Execute.
        await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);

        // Assert expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(new[] { "Foo" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
