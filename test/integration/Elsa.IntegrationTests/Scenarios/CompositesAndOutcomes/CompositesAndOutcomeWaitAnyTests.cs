using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.CompositesAndOutcomes;

public class ExplicitJoinWaitAnyTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public ExplicitJoinWaitAnyTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .WithWorkflowsFromDirectory("Scenarios", "CompositesAndOutcomes", "Workflows")
            .Build();
    }

    [Fact(DisplayName = "Complete activity must not cascade.")]
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Execute.
        var workflowState = await _services.RunWorkflowUntilEndAsync("tester");

        // Assert expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        var expectedLinesArray = new[] { "Start", "End" };
        Assert.Equal(expectedLinesArray, lines);

        // Assert expected workflow status.
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
    }
}