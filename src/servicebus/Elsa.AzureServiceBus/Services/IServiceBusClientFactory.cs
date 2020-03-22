using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.AzureServiceBus.Services
{
    public interface IServiceBusClientFactory
    {
        Microsoft.Azure.ServiceBus.Core.ISenderClient Create(string queueName);
    }
}
