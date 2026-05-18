using CShells.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.ShellFeatures;

/// <summary>
/// Adds and configures the Mediator feature.
/// </summary>
[ShellFeature(
    "Mediator",
    DisplayName = "Mediator",
    Description = "Registers mediator services for in-process notifications and requests")]
public class MediatorFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddMediator()
            .AddMediatorHostedServices();
    }
}
