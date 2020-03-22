using Elsa.AzureServiceBus.Models;
using MediatR;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.AzureServiceBus.Notifications
{
    public class ProcessMessageNotification : INotification
    {
        public string ConsumerName { get; set; }

        public string QueueName { get; set; }

        public MessageBody Message { get; set; }
    }
}
