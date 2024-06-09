using Elsa.Framework.Shells.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Framework.Shells.Services;

public class ShellFactory(IApplicationServicesAccessor applicationServicesAccessor) : IShellFactory
{
    public Shell CreateShell(ShellBlueprint blueprint)
    {
        var shellServices = applicationServicesAccessor.ApplicationServices.Clone();
        var shellServiceProvider = shellServices.BuildServiceProvider();
        return new Shell(blueprint, shellServices, shellServiceProvider);
    }
}