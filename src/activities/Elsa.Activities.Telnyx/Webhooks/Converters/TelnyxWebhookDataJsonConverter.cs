using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elsa.Activities.Telnyx.Webhooks.Attributes;
using Elsa.Activities.Telnyx.Webhooks.Models;
using Elsa.Activities.Telnyx.Webhooks.Payloads;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Activities.Telnyx.Webhooks.Converters
{
    public class TelnyxWebhookDataJsonConverter : JsonConverter
    {
        public TelnyxWebhookDataJsonConverter()
        {
            var payloadTypes = typeof(TelnyxWebhookDataJsonConverter).Assembly.GetAllWithBaseClass<Payload>();

            var query =
                from payloadType in payloadTypes
                let payloadAttribute = payloadType.GetCustomAttribute<WebhookAttribute>()
                where payloadAttribute != null
                select (payloadType, payloadAttribute);

            PayloadTypeDictionary = query.ToDictionary(x => x.payloadAttribute!.EventType, x => x.payloadType);
        }

        private IDictionary<string, Type> PayloadTypeDictionary { get; }

        public override bool CanConvert(Type objectType) => typeof(Payload).IsAssignableFrom(objectType);
        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var dataModel = JObject.Load(reader);
            var eventType = (dataModel["event_type"]?.ToString() ?? dataModel["eventType"]?.ToString() ?? dataModel["EventType"]?.ToString())!;
            var payloadType = PayloadTypeDictionary.ContainsKey(eventType) ? PayloadTypeDictionary[eventType] : typeof(UnsupportedPayload);
            var payload = (Payload) Activator.CreateInstance(payloadType)!;

            var data = new TelnyxWebhookData
            {
                Payload = payload
            };

            serializer.Populate(dataModel.CreateReader(), data);
            return data;
        }


        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) => throw new NotImplementedException();
    }
}