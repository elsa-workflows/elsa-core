using System;
using Rebus.Bus;

namespace Elsa.ServiceBus
{
    public interface IServiceBusContainer
    {
        IBus Bus { get; }
    }
}