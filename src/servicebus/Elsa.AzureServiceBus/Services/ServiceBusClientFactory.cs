using Elsa.AzureServiceBus.Options;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.AzureServiceBus.Services
{
    public class ServiceBusClientFactory : IServiceBusClientFactory
    {
        private readonly IOptions<ServiceBusOptions> _options;

        public ServiceBusClientFactory(IOptions<ServiceBusOptions> options)
        {
            _options = options;



        }

        public Microsoft.Azure.ServiceBus.Core.ISenderClient Create(string queueName)
        {
            var senderOptions = _options.Value.Sender;

            var sender = new Microsoft.Azure.ServiceBus.Core.MessageSender(senderOptions.ServiceBusConnectionString, queueName);

            if (senderOptions.UseAttachmentStorage == true && string.IsNullOrWhiteSpace(senderOptions.StorageConnectionString) == false)
            {
                var config = new AzureStorageAttachmentConfiguration(
                        senderOptions.StorageConnectionString,
                        messageMaxSizeReachedCriteria: message => senderOptions.AutoAttachmentSizeInKb > 0 ? message.Body.Length > senderOptions.AutoAttachmentSizeInKb * 1024 : false
                    )
                    .WithBlobSasUri(
                        sasTokenValidationTime: TimeSpan.FromHours(senderOptions.AttachmentLinkExpiryInHours),
                        messagePropertyToIdentifySasUri: "AttachmentSasUriProperty")
                    ;
                sender.RegisterAzureStorageAttachmentPlugin(config);
            }

            return sender;
        }


    }
}
