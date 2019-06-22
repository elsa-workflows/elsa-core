using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Serialization.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Core.Serialization
{
    public class WorkflowSerializer : IWorkflowSerializer
    {
        private readonly IDictionary<string, ITokenFormatter> formatters;
        private readonly JsonSerializer jsonSerializer;

        public WorkflowSerializer(IEnumerable<ITokenFormatter> formatters, IWorkflowSerializerProvider serializerProvider)
        {
            this.formatters = formatters.ToDictionary(x => x.Format, StringComparer.OrdinalIgnoreCase);
            jsonSerializer = serializerProvider.CreateJsonSerializer();
        }
        
        public string Serialize(WorkflowInstance workflowInstance, string format)
        {
            var token = JObject.FromObject(workflowInstance, jsonSerializer);
            return Serialize(token, format);
        }

        public string Serialize(JToken token, string format)
        {
            var formatter = formatters[format];
            return formatter.ToString(token);
        }

        public WorkflowInstance Deserialize(string data, string format)
        {
            var formatter = formatters[format];
            var token = formatter.FromString(data);
            return Deserialize(token);
        }

        public WorkflowInstance Deserialize(JToken token)
        {
            return token.ToObject<WorkflowInstance>(jsonSerializer);
        }
    }
}