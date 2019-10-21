using Microsoft.Extensions.DependencyInjection;

namespace Elsa
{
    public class ServiceConfiguration
    {
        public ServiceConfiguration(IServiceCollection services)
        {
            Services = services;
        }
        
        public IServiceCollection Services { get; }
    }
}