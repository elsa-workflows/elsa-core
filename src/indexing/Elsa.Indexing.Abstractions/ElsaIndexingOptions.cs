using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Indexing
{
    public class ElsaIndexingOptions
    {
        public ElsaIndexingOptions(IServiceCollection services, ContainerBuilder containerBuilder)
        {
            Services = services;
            ContainerBuilder = containerBuilder;
        }

        public IServiceCollection Services { get; }
        public ContainerBuilder ContainerBuilder { get; }
    }
}
