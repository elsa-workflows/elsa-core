using Elsa.Extensions;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;
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
}
