using Microsoft.Extensions.DependencyInjection;

namespace Elsa
{
    public class ElsaBuilder
    {
        public ElsaBuilder(IServiceCollection services)
        {
            Services = services;
        }
        
        public IServiceCollection Services { get; }
    }
}