using System;
using System.Text;
using Elsa.Activities.AzureServiceBus.Models;
using Elsa.Serialization;
using Microsoft.Azure.ServiceBus;

namespace Elsa.Activities.AzureServiceBus.Extensions
{
    public static class MessageBodyExtensions
    {
        public static T ReadBody<T>(this MessageModel message, IContentSerializer serializer) => (T) message.ReadBody(typeof(T), serializer);

        public static object ReadBody(this MessageModel message, Type type, IContentSerializer serializer)
        {
            if (type == typeof(string))
                return UTF8Encoding.UTF8.GetString(message.Body);

            var bytes = message.Body;
            var json = Encoding.UTF8.GetString(bytes);
            return serializer.Deserialize(json, type)!;
        }

        public static Message CreateMessage(IContentSerializer serializer, object Message)
        {
            byte[] messageBytes;    

            if (Message.GetType() == typeof(string))
                messageBytes = UTF8Encoding.UTF8.GetBytes(Message as string);
            else
            {
                var json = serializer.Serialize(Message);
                messageBytes = Encoding.UTF8.GetBytes(json);
            }

            return new Message(messageBytes);
        }

    }
}