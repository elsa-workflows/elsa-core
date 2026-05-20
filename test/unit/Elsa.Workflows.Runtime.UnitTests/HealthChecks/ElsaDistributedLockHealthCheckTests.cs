using Elsa.Workflows.Runtime.HealthChecks;
using Elsa.Workflows.Runtime.Options;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.HealthChecks;

public class ElsaDistributedLockHealthCheckTests
{
    private static readonly TimeSpan ExpectedLockAcquisitionTimeout = TimeSpan.FromMilliseconds(250);
    private readonly IDistributedLockProvider _distributedLockProvider = Substitute.For<IDistributedLockProvider>();
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly ElsaDistributedLockHealthCheck _sut;

    public ElsaDistributedLockHealthCheckTests()
    {
        _distributedLockProvider.CreateLock(Arg.Any<string>()).Returns(_distributedLock);
        _distributedLock.TryAcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<IDistributedSynchronizationHandle?>(Substitute.For<IDistributedSynchronizationHandle>()));
        _sut = new ElsaDistributedLockHealthCheck(CreateServiceProvider(_distributedLockProvider), CreateOptions(), NullLogger<ElsaDistributedLockHealthCheck>.Instance);
    }

    [Fact]
    public async Task ReturnsHealthyWhenProbeLockCanBeAcquired()
    {
        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("distributed-locks", result.Data["category"]);
        _distributedLockProvider.Received(1).CreateLock(Arg.Is<string>(x => x.StartsWith("elsa-health-check-", StringComparison.Ordinal)));
        await _distributedLock.Received(1).TryAcquireAsync(ExpectedLockAcquisitionTimeout, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UsesUniqueProbeLockNameForEachCheck()
    {
        var lockNames = new List<string>();
        var distributedLockProvider = Substitute.For<IDistributedLockProvider>();
        var distributedLock = Substitute.For<IDistributedLock>();
        distributedLockProvider.CreateLock(Arg.Do<string>(lockNames.Add)).Returns(distributedLock);
        distributedLock.TryAcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<IDistributedSynchronizationHandle?>(Substitute.For<IDistributedSynchronizationHandle>()));
        var sut = new ElsaDistributedLockHealthCheck(CreateServiceProvider(distributedLockProvider), CreateOptions(), NullLogger<ElsaDistributedLockHealthCheck>.Instance);

        await sut.CheckHealthAsync(new HealthCheckContext());
        await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(2, lockNames.Count);
        Assert.All(lockNames, x => Assert.DoesNotContain(Environment.MachineName, x, StringComparison.OrdinalIgnoreCase));
        Assert.NotEqual(lockNames[0], lockNames[1]);
    }

    [Fact]
    public async Task ReturnsDegradedWhenProbeLockCannotBeAcquired()
    {
        _distributedLock.TryAcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<IDistributedSynchronizationHandle?>((IDistributedSynchronizationHandle?)null));

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("distributed-locks", result.Data["category"]);
    }

    [Fact]
    public async Task ReturnsUnhealthyWhenProviderThrows()
    {
        _distributedLockProvider.CreateLock(Arg.Any<string>()).Returns<IDistributedLock>(_ => throw new InvalidOperationException("lock backend unavailable"));

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("distributed-locks", result.Data["category"]);
    }

    [Fact]
    public async Task ReturnsDegradedWhenProviderIsNotRegistered()
    {
        var sut = new ElsaDistributedLockHealthCheck(CreateServiceProvider(), CreateOptions(), NullLogger<ElsaDistributedLockHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("distributed-locks", result.Data["category"]);
    }

    private static IServiceProvider CreateServiceProvider(IDistributedLockProvider? distributedLockProvider = null)
    {
        var services = new ServiceCollection();

        if (distributedLockProvider != null)
            services.AddSingleton(distributedLockProvider);

        return services.BuildServiceProvider();
    }

    private static IOptions<ElsaReadinessHealthCheckOptions> CreateOptions() => Microsoft.Extensions.Options.Options.Create(new ElsaReadinessHealthCheckOptions
    {
        DistributedLockAcquisitionTimeout = ExpectedLockAcquisitionTimeout
    });
}
