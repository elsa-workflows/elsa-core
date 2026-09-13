using Elsa.Testing.Shared;

namespace Elsa.Workflows.IntegrationTests.Scenarios.CompositesAndOutcomes;

public class ExplicitJoinWaitAnyTests : IAsyncDisposable
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public ExplicitJoinWaitAnyTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .WithWorkflowsFromDirectory("Scenarios", "CompositesAndOutcomes", "Workflows")
            .Build();
    }

    [Test]
    [DisplayName("Complete activity must not cascade.")]
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Execute.
        var workflowState = await _services.RunWorkflowUntilEndAsync("tester");

        // Assert expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        var expectedLinesArray = new[] { "Start", "End" };
        await Assert.That(lines).IsEquivalentTo(expectedLinesArray, TUnit.Assertions.Enums.CollectionOrdering.Matching);

        // Assert expected workflow status.
        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
