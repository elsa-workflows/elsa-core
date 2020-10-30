using Newtonsoft.Json;
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

        [JsonExtensionData] public JObject Data { get; set; }

        public JToken? Get(string name) => Has(name) ? Data[name] : default;

        public T Get<T>(string name)
        {
            if (!Has(name))
                return default!;

            var value = Get(name);

            if (value == null)
                return default!;

            return value.ToObject<T>()!;
        }

        public Variables Set(string name, JToken value)
        {
            Data[name] = value;
            return this;
        }

        public bool Has(string name) => Data.HasKey(name);
    }
}