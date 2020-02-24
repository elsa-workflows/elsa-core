using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Serialization.Formatters;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization
{
    public class WorkflowSerializer : IWorkflowSerializer
    {
        private readonly ITokenSerializer tokenSerializer;
        private readonly IDictionary<string, ITokenFormatter> formatters;

        public WorkflowSerializer(IEnumerable<ITokenFormatter> formatters, ITokenSerializer tokenSerializer)
        {
            this.tokenSerializer = tokenSerializer;
            this.formatters = formatters.ToDictionary(x => x.Format, StringComparer.OrdinalIgnoreCase);
        }
        
        public string Serialize<T>(T workflow, string format)
        {
            var token = tokenSerializer.Serialize(workflow);
            return Serialize(token, format);
        }

        public T Deserialize<T>(string data, string format)
        {
            var formatter = formatters[format];
            var token = formatter.FromString(data);
            return Deserialize<T>(token);
        }
        
        private string Serialize(JToken token, string format)
        {
            var formatter = formatters[format];
            return formatter.ToString(token);
        }

        private T Deserialize<T>(JToken token) => tokenSerializer.Deserialize<T>(token);
    }
}