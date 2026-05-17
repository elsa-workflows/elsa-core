using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Diagnostics.ConsoleLogs.Providers.InMemory;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

public class ConsoleLogsRegistrationTests
{
    [Fact]
    public void AddConsoleLogsServices_WhenNoProviderIsConfigured_UsesInMemoryProvider()
    {
        var services = new ServiceCollection();

        services.AddConsoleLogsServices();

        using var serviceProvider = services.BuildServiceProvider();
        Assert.IsType<InMemoryConsoleLogProvider>(serviceProvider.GetRequiredService<IConsoleLogProvider>());
        Assert.IsType<ConsoleLogSourceRegistry>(serviceProvider.GetRequiredService<IConsoleLogSourceRegistry>());
        Assert.IsType<ConsoleLogRedactor>(serviceProvider.GetRequiredService<IConsoleLogRedactor>());
    }

    [Fact]
    public void AddConsoleLogsServices_RegistersHostedCapture()
    {
        var services = new ServiceCollection();

        services.AddConsoleLogsServices();

        using var serviceProvider = services.BuildServiceProvider();
        Assert.Contains(serviceProvider.GetServices<IHostedService>(), x => x.GetType() == typeof(ConsoleLogCaptureHostedService));
    }
}
