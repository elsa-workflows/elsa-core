using System;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus
{
    public interface IServiceBusContainerFactory
    {
        IServiceBusContainer CreateBusContainer(Action<IServiceCollection> configure);
    }
}