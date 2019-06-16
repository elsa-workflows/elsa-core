using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Core.Serialization
{
    public class WorkflowSerializer : IWorkflowSerializer
    {
        private readonly IDictionary<string, ITokenFormatter> formatters;
        private readonly JsonSerializer jsonSerializer;

        public WorkflowSerializer(IEnumerable<ITokenFormatter> formatters)
        {
            this.formatters = formatters.ToDictionary(x => x.Format, StringComparer.OrdinalIgnoreCase);
            jsonSerializer = new JsonSerializer();
        }
        
        public string Serialize(Workflow workflow, string format)
        {
            var token = JObject.FromObject(workflow, jsonSerializer);
            return Serialize(token, format);
        }

        public string Serialize(JToken token, string format)
        {
            var formatter = formatters[format];
            return formatter.ToString(token);
        }

        public Workflow Deserialize(string data, string format)
        {
            var formatter = formatters[format];
            var token = formatter.FromString(data);
            return Deserialize(token);
        }

        public Workflow Deserialize(JToken token)
        {
            return token.ToObject<Workflow>(jsonSerializer);
        }

        public Workflow Clone(Workflow workflow)
        {
            var token = JObject.FromObject(workflow, jsonSerializer);
            var clonedToken = token.DeepClone();
            return clonedToken.ToObject<Workflow>(jsonSerializer);
        }

        public Workflow Derive(Workflow parent)
        {
            var instance = Clone(parent);
            instance.ParentId = instance.Id;
            return instance;
        }
    }
}