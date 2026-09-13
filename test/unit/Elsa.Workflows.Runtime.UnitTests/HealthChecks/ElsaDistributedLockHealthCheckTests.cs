using Elsa.Workflows.Runtime.HealthChecks;
using Elsa.Workflows.Runtime.Options;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.HealthChecks;

public class ElsaDistributedLockHealthCheckTests : IAsyncDisposable
{
    private static readonly TimeSpan ExpectedLockAcquisitionTimeout = TimeSpan.FromMilliseconds(250);
    private readonly IDistributedLockProvider _distributedLockProvider = Substitute.For<IDistributedLockProvider>();
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly ServiceProvider _serviceProvider;
    private readonly ElsaDistributedLockHealthCheck _sut;

    public ElsaDistributedLockHealthCheckTests()
    {
        _distributedLockProvider.CreateLock(Arg.Any<string>()).Returns(_distributedLock);
        _distributedLock.TryAcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<IDistributedSynchronizationHandle?>(Substitute.For<IDistributedSynchronizationHandle>()));
        _serviceProvider = CreateServiceProvider(_distributedLockProvider);
        _sut = new ElsaDistributedLockHealthCheck(_serviceProvider, CreateOptions(), NullLogger<ElsaDistributedLockHealthCheck>.Instance);
    }

    [Test]
    public async Task ReturnsHealthyWhenProbeLockCanBeAcquired()
    {
        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
        await Assert.That(result.Data["category"]).IsEqualTo("distributed-locks");
        _distributedLockProvider.Received(1).CreateLock(Arg.Is<string>(x => IsProbeLockName(x)));
        await _distributedLock.Received(1).TryAcquireAsync(ExpectedLockAcquisitionTimeout, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UsesUniqueProbeLockNameForEachCheck()
    {
        var lockNames = new List<string>();
        var distributedLockProvider = Substitute.For<IDistributedLockProvider>();
        var distributedLock = Substitute.For<IDistributedLock>();
        distributedLockProvider.CreateLock(Arg.Do<string>(lockNames.Add)).Returns(distributedLock);
        distributedLock.TryAcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<IDistributedSynchronizationHandle?>(Substitute.For<IDistributedSynchronizationHandle>()));
        await using var serviceProvider = CreateServiceProvider(distributedLockProvider);
        var sut = new ElsaDistributedLockHealthCheck(serviceProvider, CreateOptions(), NullLogger<ElsaDistributedLockHealthCheck>.Instance);

        await sut.CheckHealthAsync(new HealthCheckContext());
        await sut.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(lockNames.Count).IsEqualTo(2);
        foreach (var lockName in lockNames)
            await Assert.That(lockName).DoesNotContain(Environment.MachineName).WithComparison(StringComparison.OrdinalIgnoreCase);
        await Assert.That(lockNames[1]).IsNotEqualTo(lockNames[0]);
        foreach (var lockName in lockNames)
            await Assert.That(IsProbeLockName(lockName)).IsTrue();
    }

    [Test]
    public async Task ReturnsDegradedWhenProbeLockCannotBeAcquired()
    {
        _distributedLock.TryAcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<IDistributedSynchronizationHandle?>((IDistributedSynchronizationHandle?)null));

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Degraded);
        await Assert.That(result.Data["category"]).IsEqualTo("distributed-locks");
    }

    [Test]
    public async Task ReturnsUnhealthyWhenProviderThrows()
    {
        _distributedLockProvider.CreateLock(Arg.Any<string>()).Returns<IDistributedLock>(_ => throw new InvalidOperationException("lock backend unavailable"));

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Unhealthy);
        await Assert.That(result.Data["category"]).IsEqualTo("distributed-locks");
    }

    [Test]
    public async Task ReturnsDegradedWhenProviderIsNotRegistered()
    {
        await using var serviceProvider = CreateServiceProvider();
        var sut = new ElsaDistributedLockHealthCheck(serviceProvider, CreateOptions(), NullLogger<ElsaDistributedLockHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Degraded);
        await Assert.That(result.Data["category"]).IsEqualTo("distributed-locks");
    }

    public ValueTask DisposeAsync() => _serviceProvider.DisposeAsync();

    private static ServiceProvider CreateServiceProvider(IDistributedLockProvider? distributedLockProvider = null)
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

    private static bool IsProbeLockName(string lockName)
    {
        const string prefix = "elsa-health-check-";
        return lockName.StartsWith(prefix, StringComparison.Ordinal)
               && Guid.TryParseExact(lockName[prefix.Length..], "N", out _);
    }
}
