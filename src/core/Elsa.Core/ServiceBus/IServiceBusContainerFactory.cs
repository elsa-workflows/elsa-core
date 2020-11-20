using System;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Config;

namespace Elsa.ServiceBus
{
    public interface IServiceBusContainerFactory
    {
        IServiceBusContainer CreateBusContainer(string name, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure);
        IServiceBusContainer GetBusContainer(string name);
    }
}