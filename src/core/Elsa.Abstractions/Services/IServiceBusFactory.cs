using System;
using Rebus.Bus;
using Rebus.Config;

namespace Elsa.Services
{
    public interface IServiceBusFactory
    {
        IBus CreateServiceBus(string name, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure);
        IBus GetServiceBus(string name);
    }
}