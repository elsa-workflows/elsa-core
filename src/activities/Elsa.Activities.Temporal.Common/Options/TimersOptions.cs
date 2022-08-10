using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Temporal.Common.Options
{
    public class TimersOptions
    {
        public TimersOptions(IServiceCollection services, ContainerBuilder containerBuilder)
        {
            Services = services;
            ContainerBuilder = containerBuilder;
        }

        public IServiceCollection Services { get; }

        public ContainerBuilder ContainerBuilder { get; }
    }
}