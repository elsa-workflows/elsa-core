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

    [Fact(DisplayName = "WorkflowDefinitionActivity should pass inputs from parent to child workflow")]
    public async Task Should_Pass_Inputs_To_Child_Workflow()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Run the parent workflow that passes an input to the child.
        await _services.RunWorkflowUntilEndAsync("parent-with-input");

        // Assert - child workflow should have received and written the input value.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Contains("Hello from parent!", lines);
    }

    [Fact(DisplayName = "WorkflowDefinitionActivity should receive outputs from child workflow")]
    public async Task Should_Receive_Outputs_From_Child_Workflow()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Run the parent workflow that receives output from the child.
        await _services.RunWorkflowUntilEndAsync("parent-with-output");

        // Assert - parent workflow should have received and written the child's output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Contains("Received from child: Child output value", lines);
    }

    [Fact(DisplayName = "WorkflowDefinitionActivity should handle workflows with multiple outcomes")]
    public async Task Should_Handle_Multiple_Outcomes()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Run the workflow that uses a child with multiple outcomes.
        await _services.RunWorkflowUntilEndAsync("parent-with-outcomes");

        // Assert - should follow the correct outcome path.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Contains("Success path taken", lines);
    }

    [Fact(DisplayName = "WorkflowDefinitionActivity should fall back to published version when specific version not found")]
    public async Task Should_Fallback_To_Published_Version()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Run a workflow that references a non-existent version (should fallback to published).
        await _services.RunWorkflowUntilEndAsync("parent-version-fallback");

        // Assert - should have executed the published version.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Contains("Published version executed", lines);
    }
}
