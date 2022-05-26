using Elsa.Mediator.Extensions;
using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Services;

namespace Elsa.Mediator.Configuration;

public class MediatorConfigurator : ConfiguratorBase
{
    public MediatorConfigurator(IServiceConfiguration serviceConfiguration) : base(serviceConfiguration)
    {
    }

    public override void Apply()
    {
        Services.AddMediator();
    }
}