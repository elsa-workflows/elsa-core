using Elsa.Extensions;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.UnitTests.HealthChecks;

public class HealthCheckExtensionsTests
{
    [Test]
    public async Task AddElsaReadinessChecksRegistersReadinessOptions()
    {
        var services = new ServiceCollection();

        services
            .AddHealthChecks()
            .AddElsaReadinessChecks(includePersistence: false, includeDistributedLocks: true);

        using var serviceProvider = services.BuildServiceProvider();

        await Assert.That(serviceProvider.GetRequiredService<IOptions<ElsaReadinessHealthCheckOptions>>()).IsNotNull();
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public void AddElsaReadinessChecksRejectsNonPositiveDistributedLockTimeout(int timeoutMilliseconds)
    {
        var services = new ServiceCollection();

        services
            .AddHealthChecks()
            .AddElsaReadinessChecks(
                includePersistence: false,
                includeDistributedLocks: true,
                configureOptions: options => options.DistributedLockAcquisitionTimeout = TimeSpan.FromMilliseconds(timeoutMilliseconds));

        using var serviceProvider = services.BuildServiceProvider();

        Assert.ThrowsExactly<OptionsValidationException>(() => _ = serviceProvider.GetRequiredService<IOptions<ElsaReadinessHealthCheckOptions>>().Value);
    }

    [Test]
    public async Task AddElsaReadinessChecksUsesElsaSpecificReadinessTag()
    {
        var services = new ServiceCollection();

        services
            .AddHealthChecks()
            .AddElsaReadinessChecks(includePersistence: false);

        using var serviceProvider = services.BuildServiceProvider();
        var registrations = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        var registration = await Assert.That(registrations).HasSingleItem();
        await Assert.That(registration.Tags).Contains("elsa");
        await Assert.That(registration.Tags).Contains(HealthCheckExtensions.ReadinessTag);
        await Assert.That(registration.Tags).DoesNotContain("readiness");
    }
}
