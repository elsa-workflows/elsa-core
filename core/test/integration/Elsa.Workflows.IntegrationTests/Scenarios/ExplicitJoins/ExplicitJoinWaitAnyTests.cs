using Elsa.Testing.Shared;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ExplicitJoins;

public class ExplicitJoinWaitAnyTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public ExplicitJoinWaitAnyTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .Build();
    }

    [Theory(DisplayName = "Workflows with explicit joins complete the workflow.")]
    [InlineData("join-any-1.json", "Start; End")]
    [InlineData("join-all-1.json", "Start; Line 1; Line 2; End")]
    [InlineData("join-all-2.json", "Start; Line 1; Line 2; End")]
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
        Assert.Equal(expectedLinesArray, lines);

        // Assert expected workflow status.
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
    }
}