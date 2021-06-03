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

            if(message is MessageModel)
            {
                var messageModel = (MessageModel)message;
                return CreateMessageFromMessageModel(messageModel);
            }
            else if (message is string s)
                messageBytes = Encoding.UTF8.GetBytes(s);
            else
            {
                var json = serializer.Serialize(message);
                messageBytes = Encoding.UTF8.GetBytes(json);
            }

            return new Message(messageBytes);
        }

        private static Message CreateMessageFromMessageModel(MessageModel message)
        {
            var returnMessage = new Message(message.Body)
            {
                CorrelationId = message.CorrelationId,
                ContentType = message.ContentType,
                Label = message.Label,
                To = message.To,
                PartitionKey = message.PartitionKey,
                ViaPartitionKey = message.ViaPartitionKey,
                ReplyTo = message.ReplyTo,
                SessionId = message.SessionId,
                ReplyToSessionId = message.ReplyToSessionId,
            };

            if(message.MessageId != null)
                returnMessage.MessageId = message.MessageId;
            if(message.TimeToLive != null && message.TimeToLive > TimeSpan.Zero )
                returnMessage.TimeToLive = message.TimeToLive;
            if(message.ScheduledEnqueueTimeUtc != null)
                returnMessage.ScheduledEnqueueTimeUtc = message.ScheduledEnqueueTimeUtc;
            
            if (message.UserProperties != null)
                foreach (var props in message.UserProperties)
                    returnMessage.UserProperties.Add(props.Key, props.Value);

            return returnMessage;
        }

    }
}