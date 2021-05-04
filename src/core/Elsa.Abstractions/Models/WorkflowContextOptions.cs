using System;
using System.ComponentModel;
using Elsa.Converters;
using Elsa.Serialization.Converters;
using Newtonsoft.Json;

namespace Elsa.Models
{
    public class WorkflowContextOptions
    {
        [JsonConverter(typeof(TypeJsonConverter))]
        [TypeConverter(typeof(TypeTypeConverter))]
        public Type? ContextType { get; set; }
        public WorkflowContextFidelity ContextFidelity { get; set; }
    }
}