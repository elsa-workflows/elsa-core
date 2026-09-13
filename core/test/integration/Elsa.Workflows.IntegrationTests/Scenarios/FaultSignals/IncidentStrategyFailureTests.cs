using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Testing.Shared.Activities;
using Elsa.Workflows.Activities;
using Elsa.Workflows.IntegrationTests.Scenarios.FaultSignals.Activities;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.FaultSignals;

/// <summary>
/// Pins the behavior described in issue #7914: when an <see cref="IIncidentStrategy"/> throws, the activity-level
/// exception middleware must catch it, attribute the resulting incident to the faulting activity (not the workflow
/// root), and keep the workflow from being misattributed via the workflow-level middleware's catch.
/// </summary>
public class IncidentStrategyFailureTests(ITestOutputHelper testOutputHelper)
{
    private readonly Fault _faultingActivity = Fault.Create("Whoops!", "Test", "Test");

    [Fact(DisplayName = "An incident strategy that throws records the failure against the faulting activity")]
    public async Task StrategyThatThrows_AttributesTheIncidentToTheFaultingActivity()
    {
        // Arrange
        var container = new TestContainer
        {
            Activities = { _faultingActivity }
        };

        var fixture = new WorkflowTestFixture(testOutputHelper);
        fixture.ConfigureServices(services =>
        {
            services.Configure<IncidentOptions>(o => o.DefaultIncidentStrategy = typeof(ThrowingIncidentStrategy));
        });

        // Act
        var result = await fixture.RunWorkflowAsync(new TestWorkflow(builder =>
        {
            builder.Root = container;
        }));

        // Assert: the workflow did not get misattributed by the workflow-level middleware because the activity-level
        // middleware swallowed the strategy's exception. The original fault incident and the strategy failure are
        // both attributed to the faulting activity, and no workflow-root fallback incident was added.
        Assert.Equal(2, result.WorkflowState.Incidents.Count);
        Assert.All(result.WorkflowState.Incidents, incident => Assert.Equal(_faultingActivity.Id, incident.ActivityId));

        var strategyIncident = Assert.Single(result.WorkflowState.Incidents, x => x.Message == "ThrowingIncidentStrategy");
        Assert.Equal("ThrowingIncidentStrategy", strategyIncident.Message);
    }

    [Fact(DisplayName = "Cancellation from an incident strategy propagates unchanged")]
    public async Task StrategyThatCancels_PropagatesCancellation()
    {
        // The guard around HandleIncidentAsync deliberately excludes OperationCanceledException, mirroring the guard
        // around TrySendSignalAsync. Cancellation means the host is tearing the run down, not that the strategy is
        // broken, and swallowing it would turn a deliberate cancellation into a misattributed incident.
        var container = new TestContainer
        {
            Activities = { _faultingActivity }
        };

        var fixture = new WorkflowTestFixture(testOutputHelper);
        fixture.ConfigureServices(services =>
        {
            services.Configure<IncidentOptions>(o => o.DefaultIncidentStrategy = typeof(CancellingIncidentStrategy));
        });

        var result = await fixture.RunWorkflowAsync(new TestWorkflow(builder =>
        {
            builder.Root = container;
        }));

        // Assert: cancellation escaped the activity-level middleware and the workflow-level middleware cancelled the
        // run. Incident bookkeeping is unchanged from the existing cancellation path.
        Assert.Equal(WorkflowSubStatus.Cancelled, result.WorkflowState.SubStatus);
    }

    /// <summary>
    /// A test-only <see cref="IIncidentStrategy"/> that throws a non-cancellation exception.
    /// </summary>
    private sealed class ThrowingIncidentStrategy : IIncidentStrategy
    {
        public void HandleIncident(ActivityExecutionContext context) => throw new InvalidOperationException("ThrowingIncidentStrategy");
    }

    /// <summary>
    /// A test-only <see cref="IIncidentStrategy"/> that throws an <see cref="OperationCanceledException"/>, which
    /// should propagate instead of being captured as a misattributed incident.
    /// </summary>
    private sealed class CancellingIncidentStrategy : IIncidentStrategy
    {
        public void HandleIncident(ActivityExecutionContext context) => throw new OperationCanceledException();
    }
}
