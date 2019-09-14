using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Serialization.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization
{
    public class WorkflowSerializer : IWorkflowSerializer
    {
        private readonly IDictionary<string, ITokenFormatter> formatters;

        public WorkflowSerializer(IEnumerable<ITokenFormatter> formatters, IWorkflowSerializerProvider serializerProvider)
        {
            this.formatters = formatters.ToDictionary(x => x.Format, StringComparer.OrdinalIgnoreCase);
            Serializer = serializerProvider.CreateJsonSerializer();
        }
        
        public JsonSerializer Serializer { get; }

        public string Serialize<T>(T workflowInstance, string format)
        {
            var token = JObject.FromObject(workflowInstance, Serializer);
            return Serialize((JToken)token, format);
        }

        public string Serialize(JToken token, string format)
        {
            var formatter = formatters[format];
            return formatter.ToString(token);
        }

        public T Deserialize<T>(string data, string format)
        {
            var formatter = formatters[format];
            var token = formatter.FromString(data);
            return Deserialize<T>(token);
        }

        public T Deserialize<T>(JToken token)
        {
            return token.ToObject<T>(Serializer);
        }
    }
}