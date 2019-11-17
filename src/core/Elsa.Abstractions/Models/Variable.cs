using Newtonsoft.Json;

namespace Elsa.Models
{
    [JsonObject(ItemTypeNameHandling = TypeNameHandling.Auto)]
    public class Variable
    {
        public Variable()
        {
        }

        public Variable(object value)
        {
            Value = value;
        }
        
        public object Value { get; set; }
    }
}