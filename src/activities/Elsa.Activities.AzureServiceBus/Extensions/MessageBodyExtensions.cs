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
                return Encoding.UTF8.GetString(message.Body);

            var bytes = message.Body;
            var json = Encoding.UTF8.GetString(bytes);
            return serializer.Deserialize(json, type)!;
        }

        public static Message CreateMessage(IContentSerializer serializer, object message)
        {
            byte[] messageBytes;    

            if (message is string s)
                messageBytes = Encoding.UTF8.GetBytes(s);
            else
            {
                var json = serializer.Serialize(message);
                messageBytes = Encoding.UTF8.GetBytes(json);
            }

            return new Message(messageBytes);
        }
    }
}