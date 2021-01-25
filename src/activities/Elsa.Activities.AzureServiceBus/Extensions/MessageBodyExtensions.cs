using System;
using System.Text;
using Elsa.Activities.AzureServiceBus.Models;
using Elsa.Serialization;

namespace Elsa.Activities.AzureServiceBus.Extensions
{
    public static class MessageBodyExtensions
    {
        public static T ReadBody<T>(this MessageModel message, IContentSerializer serializer) => (T) message.ReadBody(typeof(T), serializer);

        public static object ReadBody(this MessageModel message, Type type, IContentSerializer serializer)
        {
            var bytes = message.Body;
            var json = Encoding.UTF8.GetString(bytes);
            return serializer.Deserialize(json, type)!;
        }
    }
}