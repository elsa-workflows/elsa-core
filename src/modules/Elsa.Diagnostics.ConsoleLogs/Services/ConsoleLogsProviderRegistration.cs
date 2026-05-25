using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

internal sealed class ConsoleLogsProviderRegistration(IServiceCollection services, ServiceDescriptor defaultProviderDescriptor)
{
    public void ConfigureProvider(IServiceProvider serviceProvider)
    {
        if (!HasCustomProvider())
            return;

        ConsoleLogsHost.ConfigureProvider((_, _) => serviceProvider.GetRequiredService<IConsoleLogProvider>());
    }

    private bool HasCustomProvider() =>
        services.Any(x => x.ServiceType == typeof(IConsoleLogProvider) && !ReferenceEquals(x, defaultProviderDescriptor));
}
