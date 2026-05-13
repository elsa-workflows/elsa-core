using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Extensions;
using Elsa.Diagnostics.StructuredLogs.Providers.InMemory;
using Elsa.Diagnostics.StructuredLogs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Diagnostics.StructuredLogs.UnitTests;

public class StructuredLogsStorageRegistrationTests
{
    [Fact]
    public void AddStructuredLogsServices_WhenNoStoreIsConfigured_UsesInMemoryStorageAndComposedProvider()
    {
        var services = new ServiceCollection();

        services.AddStructuredLogsServices();

        using var serviceProvider = services.BuildServiceProvider();
        Assert.IsType<InMemoryStructuredLogStore>(serviceProvider.GetRequiredService<IStructuredLogStore>());
        Assert.IsType<InMemoryStructuredLogLiveFeed>(serviceProvider.GetRequiredService<IStructuredLogLiveFeed>());
        Assert.IsType<DefaultStructuredLogProvider>(serviceProvider.GetRequiredService<IStructuredLogProvider>());
    }

    [Fact]
    public void StructuredLogsAssembly_DoesNotReferenceSqlitePersistence()
    {
        var references = typeof(IStructuredLogProvider)
            .Assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .ToList();

        Assert.DoesNotContain("Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite", references);
    }

    [Fact]
    public void AddStructuredLogsServices_RegistersLoggerProvider()
    {
        var services = new ServiceCollection();

        services.AddStructuredLogsServices();

        using var serviceProvider = services.BuildServiceProvider();
        Assert.Contains(serviceProvider.GetServices<ILoggerProvider>(), x => x.GetType().Name == "StructuredLogLoggerProvider");
    }
}
