using Elsa.Testing.Shared;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.IntegrationTests.Scenarios.Incidents.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.Incidents;

public class IncidentStrategyTests
{
    [Test]
    [MethodDataSource(nameof(IncidentStrategyCases))]
    public async Task Test0(Type incidentStrategyType, string[] expectedOutput, WorkflowSubStatus expectedSubStatus)
    {
        var capturingTextWriter = new CapturingTextWriter();
        await using var services = (ServiceProvider)new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .WithCapturingTextWriter(capturingTextWriter)
            .ConfigureServices(serviceCollection => serviceCollection.AddSingleton(new FaultyWorkflowOptions(incidentStrategyType)))
            .AddWorkflow<FaultyWorkflow>()
            .Build();
        await services.PopulateRegistriesAsync();
        var workflowState = await services.RunWorkflowUntilEndAsync<FaultyWorkflow>();
        var lines = capturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(expectedOutput, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(workflowState.SubStatus).IsEqualTo(expectedSubStatus);
        await Assert.That(workflowState.Incidents).HasSingleItem();
    }

    public static IEnumerable<TestDataRow<Func<(Type IncidentStrategyType, string[] ExpectedOutput, WorkflowSubStatus ExpectedSubStatus)>>> IncidentStrategyCases()
    {
        yield return new(
            static () => (typeof(FaultStrategy), ["Start", "Step 1a", "Step 2a"], WorkflowSubStatus.Faulted),
            DisplayName: "Workflows with a Fault strategy do not continue after a fault: FaultStrategy");
        yield return new(
            static () => (typeof(ContinueWithIncidentsStrategy), ["Start", "Step 1a", "Step 2a", "Step 1b"], WorkflowSubStatus.Suspended),
            DisplayName: "Workflows with a Fault strategy do not continue after a fault: ContinueWithIncidentsStrategy");
    }
}
