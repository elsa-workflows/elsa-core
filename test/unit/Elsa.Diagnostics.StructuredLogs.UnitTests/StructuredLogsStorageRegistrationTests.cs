using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Extensions;
using Elsa.Diagnostics.StructuredLogs.Providers.InMemory;
using Elsa.Diagnostics.StructuredLogs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.StructuredLogs.UnitTests;

public class StructuredLogsStorageRegistrationTests
{
    [Test]
    public async Task AddStructuredLogsServices_WhenNoStoreIsConfigured_UsesInMemoryStorageAndComposedProvider()
    {
        var services = new ServiceCollection();

        services.AddStructuredLogsServices();

        using var serviceProvider = services.BuildServiceProvider();
        await Assert.That(serviceProvider.GetRequiredService<IStructuredLogStore>()).IsOfType(typeof(InMemoryStructuredLogStore));
        await Assert.That(serviceProvider.GetRequiredService<IStructuredLogLiveFeed>()).IsOfType(typeof(InMemoryStructuredLogLiveFeed));
        await Assert.That(serviceProvider.GetRequiredService<IStructuredLogProvider>()).IsOfType(typeof(DefaultStructuredLogProvider));
    }

    [Test]
    public async Task StructuredLogsAssembly_DoesNotReferenceSqlitePersistence()
    {
        var references = typeof(IStructuredLogProvider)
            .Assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .ToList();

        await Assert.That(references).DoesNotContain("Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite");
    }

    [Test]
    public async Task AddStructuredLogsServices_RegistersLoggerProvider()
    {
        var services = new ServiceCollection();

        services.AddStructuredLogsServices();

        using var serviceProvider = services.BuildServiceProvider();
        await Assert.That(serviceProvider.GetServices<ILoggerProvider>()).Contains(x => x.GetType().Name == "StructuredLogLoggerProvider");
    }
}
