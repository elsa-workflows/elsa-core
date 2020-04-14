using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.AzureServiceBus.Models
{
    public class ServiceBusSignalMessage<T>
    {
        public Dictionary<string, string> Actions { get; set; }
        public T Data { get; set; }
    }
}
