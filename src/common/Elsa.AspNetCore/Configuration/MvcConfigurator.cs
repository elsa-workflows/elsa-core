using Elsa.AspNetCore.Conventions;
using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AspNetCore.Configuration;

public class MvcConfigurator : ConfiguratorBase
{
    public MvcConfigurator(IServiceConfiguration serviceConfiguration) : base(serviceConfiguration)
    {
    }

    public override void Configure()
    {
        Services.AddMvc(mvc => mvc.Conventions.Add(new ApiEndpointAttributeConvention()));
    }
}