using Elsa.Testing.Shared;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDefinitionActivities;

public class Tests : IAsyncDisposable
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .WithWorkflowsFromDirectory("Scenarios", "WorkflowDefinitionActivities", "Workflows")
            .Build();
    }

    [Test]
    [MethodDataSource(nameof(WorkflowDefinitionActivityTestCases))]
    public async Task Should_Execute_WorkflowDefinitionActivity_Scenarios(string workflowDefinitionId, string expectedOutput)
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Run the workflow.
        await _services.RunWorkflowUntilEndAsync(workflowDefinitionId);

        // Assert - verify expected output appears.
        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).Contains(expectedOutput);
    }

    public static IEnumerable<Func<(string WorkflowDefinitionId, string ExpectedOutput)>> WorkflowDefinitionActivityTestCases()
    {
        yield return static () => ("parent-with-input", "Hello from parent!");
        yield return static () => ("parent-with-output", "Received from child: Child output value");
        yield return static () => ("parent-with-outcomes", "Success path taken");
        yield return static () => ("parent-version-fallback", "Published version executed");
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
