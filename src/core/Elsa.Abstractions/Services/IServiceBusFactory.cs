using System;
using Rebus.Bus;

namespace Elsa.Services
{
    public interface IServiceBusFactory
    {
        IBus GetServiceBus(Type messageType);
    }
}