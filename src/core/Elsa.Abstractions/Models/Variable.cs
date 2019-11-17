using Elsa.Converters;
using Newtonsoft.Json;

namespace Elsa.Models
{
    public class Variable
    {
        public Variable()
        {
        }

        public Variable(object value)
        {
            Value = value;
        }
    
        [JsonConverter(typeof(TypeNameHandlingConverter))]
        public object Value { get; set; }
    }
}