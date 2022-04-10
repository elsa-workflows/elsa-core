using Elsa.ServiceBus.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Modules.MassTransit.Services;

/// <summary>
/// A MassTransit implementation for <see cref="IServiceBus"/>.
/// </summary>
public class MassTransitServiceBusProvider : IServiceBusProvider
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<IServiceBus, MassTransitServiceBus>();
    }
}