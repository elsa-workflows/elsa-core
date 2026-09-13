using Elsa.Testing.Shared;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ExplicitJoins;

public class ExplicitJoinWaitAnyTests : IAsyncDisposable
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public ExplicitJoinWaitAnyTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .Build();
    }

    [Test]
    [DisplayName("Workflows with explicit joins complete the workflow: $filename")]
    [Arguments("join-any-1.json", "Start; End")]
    [Arguments("join-all-1.json", "Start; Line 1; Line 2; End")]
    [Arguments("join-all-2.json", "Start; Line 1; Line 2; End")]
    public async Task Test1(string filename, string expectedLines)
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var fileName = $"Scenarios/ExplicitJoins/Workflows/{filename}";
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync(fileName);

        // Execute.
        var workflowState = await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);

        // Assert expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        var expectedLinesArray = expectedLines.Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        await Assert.That(lines).IsEquivalentTo(expectedLinesArray, TUnit.Assertions.Enums.CollectionOrdering.Matching);

        // Assert expected workflow status.
        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
