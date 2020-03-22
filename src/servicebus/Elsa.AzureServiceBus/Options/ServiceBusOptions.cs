using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.AzureServiceBus.Options
{
    public class ServiceBusOptions
    {
        /// <summary>
        /// Options for configuring the service bus details when sending
        /// </summary>
        public ServiceBusSenderOptions Sender { get; set; }
        /// <summary>
        /// Options for configuring the service bus details for consuming 
        /// </summary>
        public ServiceBusConsumerOptions[] Consumer { get; set; }


    }

    public class ServiceBusSenderOptions
    {
        public string ServiceBusConnectionString { get; set; }

        /// <summary>
        /// Connection string for azure blob storage to store the message when it exceeds the size limit
        /// </summary>
        public string StorageConnectionString { get; set; }

        /// <summary>
        /// True to use azure blob storage for large messages. 
        /// If using attachemtn storage the client will need to process the message specially. ie. ServiceBus.AttachmentPlugin
        /// </summary>
        public bool UseAttachmentStorage { get; set; } = true;

        /// <summary>
        /// Size in kb to auto convert the message data to an attachment and store as blob storage
        /// </summary>
        public int AutoAttachmentSizeInKb { get; set; } = 200;

        /// <summary>
        /// The time in hours the attachment link will stay active
        /// </summary>
        public int AttachmentLinkExpiryInHours { get; set; } = 4;
    }

    public class ServiceBusConsumerOptions
    {
        /// <summary>
        /// The consumer name that is referred to in the activities. 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// the connection string for the azure service bus to be consumed
        /// </summary>
        public string ServiceBusConnectionString { get; set; }

        /// <summary>
        /// The queue name on the azure service bus
        /// </summary>
        public string QueueName { get; set; }
    }

}
