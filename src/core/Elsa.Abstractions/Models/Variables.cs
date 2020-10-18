using Elsa.Serialization.Extensions;
using Newtonsoft.Json.Linq;

namespace Elsa.Models
{
    public class Variables 
    {
        public Variables()
        {
            Data = new JObject();
        }

        public Variables(Variables other) : this(other.Data)
        {
        }

        public Variables(JObject data)
        {
            Data = data;
        }

        public JObject Data { get; set; }
        
        public JToken? Get(string name) => Has(name) ? Data[name] : default;
        public T Get<T>(string name) => Has(name) ? Data.Value<T>(name) : default!;

        public Variables Set(string name, JToken value)
        {
            Data[name] = value;
            return this;
        }

        public bool Has(string name) => Data.HasKey(name);
    }
}