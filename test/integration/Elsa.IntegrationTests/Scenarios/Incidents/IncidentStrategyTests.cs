using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.IntegrationTests.Scenarios.Incidents.Statics;
using Elsa.IntegrationTests.Scenarios.Incidents.Workflows;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core;
using Elsa.Workflows.IncidentStrategies;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.Incidents;

public class IncidentStrategyTests
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public IncidentStrategyTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .AddWorkflow<FaultyWorkflow>()
            .Build();
    }

    [Theory(DisplayName = "Workflows with a Fault strategy do not continue after a fault")]
    [InlineData(typeof(FaultStrategy), new[] { "Start", "Step 1a", "Step 2a" }, WorkflowSubStatus.Faulted)]
    [InlineData(typeof(ContinueWithIncidentsStrategy), new[] { "Start", "Step 1a", "Step 2a", "Step 1b" }, WorkflowSubStatus.Suspended)]
    public async Task Test0(Type incidentStrategyType, string[] expectedOutput, WorkflowSubStatus expectedSubStatus)
    {
        TestSettings.IncidentStrategyType = incidentStrategyType;
        await _services.PopulateRegistriesAsync();
        var workflowState = await _services.RunWorkflowUntilEndAsync<FaultyWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(expectedOutput, lines);
        Assert.Equal(expectedSubStatus, workflowState.SubStatus);
        Assert.Equal(1, workflowState.Incidents.Count);
    }
}