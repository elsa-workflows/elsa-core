using Elsa.DropIns.Contracts;
using Elsa.DropIns.Options;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.DropIns.Extensions;

public static class ModuleExtensions
{
    public static IModule InstallDropIns(this IModule elsa, Action<DropInOptions>? configureOptions = default)
    {
        // Install drop-ins.
        var serviceProvider = new ServiceCollection().AddDropInInstaller(configureOptions).BuildServiceProvider();
        serviceProvider.GetRequiredService<IDropInInstaller>().Install(elsa);
        
        // Register drop-in monitor.
        elsa.Services.AddDropInMonitor(configureOptions);
        
        return elsa;
    }
}