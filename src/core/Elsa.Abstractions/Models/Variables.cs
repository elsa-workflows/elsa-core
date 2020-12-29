using System.Collections.Generic;

namespace Elsa.Models
{
    public class Variables
    {
        public Variables()
        {
            Data = new Dictionary<string, object?>();
        }

        public Variables(Variables other) : this(other.Data)
        {
        }

        public Variables(IDictionary<string, object?> data)
        {
            Data = data;
        }

        public IDictionary<string, object?> Data { get; set; }

        public object? Get(string name) => Has(name) ? Data[name] : default;
        public T? Get<T>(string name) => !Has(name) ? default : Get(name).ConvertTo<T>();

        public Variables Set(string name, object? value)
        {
            Data[name] = value;
            return this;
        }

        public bool Has(string name) => Data.ContainsKey(name);
    }
}