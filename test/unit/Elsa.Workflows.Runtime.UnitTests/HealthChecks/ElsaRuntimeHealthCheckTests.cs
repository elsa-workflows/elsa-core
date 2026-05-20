using Elsa.Workflows.Runtime.HealthChecks;
using Elsa.Workflows.Runtime.Services;
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

    [Fact]
    public async Task ReturnsHealthyWhenRuntimeAcceptsNewWork()
    {
        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("runtime", result.Data["category"]);
        Assert.True((bool)result.Data["acceptingNewWork"]);
    }

    [Fact]
    public async Task ReturnsDegradedWhenRuntimeIsPaused()
    {
        _quiescenceSignal.CurrentState.Returns(QuiescenceState.Initial("test") with
        {
            Reason = QuiescenceReason.AdministrativePause
        });

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("AdministrativePause", result.Data["reason"]);
        Assert.False((bool)result.Data["acceptingNewWork"]);
    }

    [Fact]
    public async Task ReturnsUnhealthyWhenRuntimeClientCannotBeCreated()
    {
        _workflowRuntime.CreateClientAsync(Arg.Any<CancellationToken>()).Returns<ValueTask<IWorkflowClient>>(_ => throw new InvalidOperationException("boom"));

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("runtime", result.Data["category"]);
    }
}
