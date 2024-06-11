using Elsa.Framework.Shells.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Framework.Shells.Services;

public class ShellFactory(IRootServicesAccessor rootServicesAccessor, IServiceProvider serviceProvider) : IShellFactory
{
    public Shell CreateShell(ShellBlueprint blueprint)
    {
        var clonedServices = rootServicesAccessor.RootServices.Clone();
        var featureTypes = blueprint.Features;

        foreach (var featureType in featureTypes)
        {
            var feature = (IShellFeature)ActivatorUtilities.CreateInstance(serviceProvider, featureType);
            feature.ConfigureServices(clonedServices);
        }

        var shellServiceProvider = clonedServices.BuildServiceProvider();
        return new Shell(blueprint, clonedServices, shellServiceProvider);
    }
}