using Elsa.ServiceBus.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MassTransit.Implementations;

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