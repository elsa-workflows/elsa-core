using Elsa.Testing.Shared;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDefinitionActivities;

public class Tests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .WithWorkflowsFromDirectory("Scenarios", "WorkflowDefinitionActivities", "Workflows")
            .Build();
    }

    [Theory]
    [MemberData(nameof(WorkflowDefinitionActivityTestCases))]
    public async Task Should_Execute_WorkflowDefinitionActivity_Scenarios(string workflowDefinitionId, string expectedOutput)
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Run the workflow.
        await _services.RunWorkflowUntilEndAsync(workflowDefinitionId);

        // Assert - verify expected output appears.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Contains(expectedOutput, lines);
    }

    public static IEnumerable<object[]> WorkflowDefinitionActivityTestCases()
    {
        yield return ["parent-with-input", "Hello from parent!"];
        yield return ["parent-with-output", "Received from child: Child output value"];
        yield return ["parent-with-outcomes", "Success path taken"];
        yield return ["parent-version-fallback", "Published version executed"];
    }
}
