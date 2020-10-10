using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Serialization.Formatters;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization
{
    public class WorkflowSerializer : IWorkflowSerializer
    {
        private readonly ITokenSerializer _tokenSerializer;
        private readonly IDictionary<string, ITokenFormatter> _formatters;

        public WorkflowSerializer(IEnumerable<ITokenFormatter> formatters, ITokenSerializer tokenSerializer)
        {
            _tokenSerializer = tokenSerializer;
            _formatters = formatters.ToDictionary(x => x.Format, StringComparer.OrdinalIgnoreCase);
        }
        
        public string Serialize<T>(T workflow, string format)
        {
            var token = _tokenSerializer.Serialize(workflow);
            return Serialize(token, format);
        }

        public T Deserialize<T>(string data, string format)
        {
            var formatter = _formatters[format];
            var token = formatter.FromString(data);
            return Deserialize<T>(token);
        }
        
        private string Serialize(JObject token, string format)
        {
            var formatter = _formatters[format];
            return formatter.ToString(token);
        }

        private T Deserialize<T>(JObject token) => _tokenSerializer.Deserialize<T>(token);
    }
}