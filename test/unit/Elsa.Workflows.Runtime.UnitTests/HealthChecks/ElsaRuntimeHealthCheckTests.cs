using Elsa.Workflows.Runtime.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.HealthChecks;

public class ElsaRuntimeHealthCheckTests
{
    private readonly IWorkflowRuntime _workflowRuntime = Substitute.For<IWorkflowRuntime>();
    private readonly IQuiescenceSignal _quiescenceSignal = Substitute.For<IQuiescenceSignal>();
    private readonly ElsaRuntimeHealthCheck _sut;

    public ElsaRuntimeHealthCheckTests()
    {
        _workflowRuntime.CreateClientAsync(Arg.Any<CancellationToken>()).Returns(new ValueTask<IWorkflowClient>(Substitute.For<IWorkflowClient>()));
        _quiescenceSignal.CurrentState.Returns(QuiescenceState.Initial("test"));
        _quiescenceSignal.ActiveExecutionCycleCount.Returns(0);
        _sut = new ElsaRuntimeHealthCheck(_workflowRuntime, _quiescenceSignal, NullLogger<ElsaRuntimeHealthCheck>.Instance);
    }

    [Test]
    public async Task ReturnsHealthyWhenRuntimeAcceptsNewWork()
    {
        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
        await Assert.That(result.Data["category"]).IsEqualTo("runtime");
        await Assert.That((bool)result.Data["acceptingNewWork"]).IsTrue();
    }

    [Test]
    public async Task ReturnsDegradedWhenRuntimeIsPaused()
    {
        _quiescenceSignal.CurrentState.Returns(QuiescenceState.Initial("test") with
        {
            Reason = QuiescenceReason.AdministrativePause
        });

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Degraded);
        await Assert.That(result.Data["reason"]).IsEqualTo("AdministrativePause");
        await Assert.That((bool)result.Data["acceptingNewWork"]).IsFalse();
    }

    [Test]
    public async Task ReturnsUnhealthyWhenRuntimeClientCannotBeCreated()
    {
        _workflowRuntime.CreateClientAsync(Arg.Any<CancellationToken>()).Returns<ValueTask<IWorkflowClient>>(_ => throw new InvalidOperationException("boom"));

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Unhealthy);
        await Assert.That(result.Data["category"]).IsEqualTo("runtime");
    }
}
