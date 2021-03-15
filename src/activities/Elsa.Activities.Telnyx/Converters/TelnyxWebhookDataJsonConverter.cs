using System;
using Elsa.Activities.Telnyx.Models;
using Elsa.Activities.Telnyx.Payloads;
using Elsa.Activities.Telnyx.Payloads.Abstract;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Activities.Telnyx.Converters
{
    public class TelnyxWebhookDataJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(Payload).IsAssignableFrom(objectType);
        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var dataModel = JObject.Load(reader);
            var eventType = (string)dataModel["event_type"]!;

            Payload payload = eventType switch
            {
                CallInitiatedPayload.EventType => new CallInitiatedPayload(),
                CallHangupPayload.EventType => new CallHangupPayload(),
                _ => new UnsupportedPayload()
            };

            var data = new TelnyxWebhookData
            {
                Payload = payload
            };
            
            serializer.Populate(dataModel.CreateReader(), data);
            return data;
        }

        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
    }
}