using System.Reflection;
using Elsa.DropIns.Contracts;
using Elsa.DropIns.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.DropIns.Services;

public class DropInStarter : IDropInStarter
{
    private readonly ITypeFinder _typeFinder;
    private readonly IServiceProvider _serviceProvider;

    public DropInStarter(ITypeFinder typeFinder, IServiceProvider serviceProvider)
    {
        _typeFinder = typeFinder;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(Assembly assembly, CancellationToken cancellationToken = default)
    {
        var startupTypes = _typeFinder.FindImplementationsOf<IDropInStartup>(assembly).ToList();

        foreach (var type in startupTypes)
        {
            var startup = (IDropInStartup)ActivatorUtilities.CreateInstance(_serviceProvider, type);
            await startup.StartAsync(cancellationToken);
        }
    }
}