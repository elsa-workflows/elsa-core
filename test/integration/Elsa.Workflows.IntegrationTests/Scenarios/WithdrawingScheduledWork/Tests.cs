using Elsa.Testing.Shared;
using Elsa.Workflows.IntegrationTests.Scenarios.WithdrawingScheduledWork.Activities;
using Elsa.Workflows.IntegrationTests.Scenarios.WithdrawingScheduledWork.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WithdrawingScheduledWork;

/// <summary>
/// A container that schedules a child and then decides the child must not run has to be able to withdraw it. Without
/// that, the container tears a branch down and an activity from that branch executes anyway, side effects and all.
/// </summary>
public class WithdrawingScheduledWorkTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;
    private readonly IWorkflowRunner _workflowRunner;

    public WithdrawingScheduledWorkTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .AddActivitiesFrom<SpeculativeScheduler>()
            .Build();

        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "A child cancelled before it was invoked does not execute, and the container's other work still does.")]
    public async Task CancellingAScheduledChildBeforeInvocationWithdrawsIt()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<SpeculativeSchedulingWorkflow>();

        Assert.Equal(["Start", "Committed", "End"], _capturingTextWriter.Lines.ToList());
    }

    [Fact(DisplayName = "Work scheduled by a container that then cancels itself does not execute.")]
    public async Task CancellingAContainerWithdrawsTheWorkItScheduled()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<SelfCancellingSchedulingWorkflow>();

        // "End" is absent because cancelling the container clears the completion callback the Sequence was waiting
        // on: the point of the assertion is that "Withdrawn" never ran.
        Assert.Equal(["Start"], _capturingTextWriter.Lines.ToList());
    }
}
