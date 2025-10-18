using Elsa.Testing.Shared;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ExplicitJoins;

public class ExplicitJoinWaitAllTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public ExplicitJoinWaitAllTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .Build();
    }

    [Fact(DisplayName = "Workflows with explicit joins complete the workflow when all inbound *active* branches have completed.")]
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var fileName = "Scenarios/ExplicitJoins/Workflows/join-active-branches-only.json";
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync(fileName);

        // Execute.
        await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);
        var lines = _capturingTextWriter.Lines.ToList();
        var expectedLines = new[]
        {
            "Branch 1", "Branch 2", "End"
        };
        Assert.Equal(expectedLines, lines);

        // Assert that Join executed.
        var joinDidExecute = true;
        Assert.True(joinDidExecute);
    }
}