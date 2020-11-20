using System;
using Rebus.Bus;

namespace Elsa.ServiceBus
{
    public interface IServiceBusContainer
    {
        IServiceProvider ServiceProvider { get; }
        IBus Bus { get; }
    }
}