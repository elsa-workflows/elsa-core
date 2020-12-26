using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using NodaTime;
using NodaTime.Text;

namespace Elsa.Client.Models
{
    [DataContract]
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

        [DataMember(Order = 1)] public IDictionary<string, object?> Data { get; set; }

        public object? Get(string name) => Has(name) ? Data[name] : default;

        public T? Get<T>(string name)
        {
            if (!Has(name))
                return default!;

            var value = Get(name);

            if (value == null)
                return default!;

            if (value is T convertedValue)
                return convertedValue;

            if (value == default!)
                return default!;

            if (typeof(T) == typeof(Duration))
                return (T?)((object?)DurationPattern.JsonRoundtrip.Parse(value!.ToString()).Value)!;
            
            var converter = TypeDescriptor.GetConverter(typeof(T));
            return converter.CanConvertFrom(value.GetType()) ? (T?) converter.ConvertFrom(value) : default;
        }

        public Variables Set(string name, object? value)
        {
            Data[name] = value;
            return this;
        }

        public bool Has(string name) => Data.ContainsKey(name);
    }
}