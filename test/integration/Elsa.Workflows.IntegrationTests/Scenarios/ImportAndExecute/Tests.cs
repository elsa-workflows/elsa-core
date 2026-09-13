using Elsa.Testing.Shared;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ImportAndExecute;

public class Tests : IAsyncDisposable
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .Build();
    }

    [Test]
    [DisplayName("Workflow imported from file should execute successfully: $workflowFileName")]
    [MethodDataSource(nameof(GetSpecimen))]
    public async Task Test1(string workflowFileName, string[] expectedOutput)
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync($"Scenarios/ImportAndExecute/Workflows/{workflowFileName}");

        // Execute.
        await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);

        // Assert.
        var lines = _capturingTextWriter.Lines.ToList();

        await Assert.That(lines).IsEquivalentTo(expectedOutput, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }
    
    public static IEnumerable<Func<(string WorkflowFileName, string[] ExpectedOutput)>> GetSpecimen()
    {
        yield return static () => ("writeline.json", ["Dummy Text"]);
        yield return static () => ("implicit-loop.json", ["Do something", "Retry", "Do something", "Done"]);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
