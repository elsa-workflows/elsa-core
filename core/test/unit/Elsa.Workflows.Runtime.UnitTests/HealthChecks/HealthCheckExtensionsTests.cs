using Elsa.Extensions;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.UnitTests.HealthChecks;

public class HealthCheckExtensionsTests
{
    [Fact]
    public void AddElsaReadinessChecksRegistersReadinessOptions()
    {
        var services = new ServiceCollection();

        services
            .AddHealthChecks()
            .AddElsaReadinessChecks(includePersistence: false, includeDistributedLocks: true);

        using var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetRequiredService<IOptions<ElsaReadinessHealthCheckOptions>>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
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

        Assert.Throws<OptionsValidationException>(() => serviceProvider.GetRequiredService<IOptions<ElsaReadinessHealthCheckOptions>>().Value);
    }

    [Fact]
    public void AddElsaReadinessChecksUsesElsaSpecificReadinessTag()
    {
        var services = new ServiceCollection();

        services
            .AddHealthChecks()
            .AddElsaReadinessChecks(includePersistence: false);

        using var serviceProvider = services.BuildServiceProvider();
        var registrations = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        var registration = Assert.Single(registrations);
        Assert.Contains("elsa", registration.Tags);
        Assert.Contains(HealthCheckExtensions.ReadinessTag, registration.Tags);
        Assert.DoesNotContain("readiness", registration.Tags);
    }
}
