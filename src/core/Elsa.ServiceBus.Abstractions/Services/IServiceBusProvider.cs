using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.Abstractions.Services;

public interface IServiceBusProvider
{
    void ConfigureServices(IServiceCollection services);
}