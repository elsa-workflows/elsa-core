using System;
using System.Text;
using Azure.Messaging.ServiceBus;
using Elsa.Activities.AzureServiceBus.Models;
using Elsa.Serialization;

namespace Elsa.Activities.AzureServiceBus.Extensions
{
    public static class MessageBodyExtensions
    {
        public static T ReadBody<T>(this MessageModel message, IContentSerializer serializer) => (T)message.ReadBody(typeof(T), serializer);

        public static object ReadBody(this MessageModel message, Type type, IContentSerializer serializer)
        {
            if (type == typeof(string))
                return Encoding.UTF8.GetString(message.Body);

            var bytes = message.Body;
            var json = Encoding.UTF8.GetString(bytes);
            return serializer.Deserialize(json, type)!;
        }

        public static ServiceBusMessage CreateMessage(IContentSerializer serializer, object message)
        {
            if (message is string s)
                return new ServiceBusMessage(s);
            
            var json = serializer.Serialize(message);
            var messageBytes = Encoding.UTF8.GetBytes(json);

            return new ServiceBusMessage(messageBytes);
        }
    }
}