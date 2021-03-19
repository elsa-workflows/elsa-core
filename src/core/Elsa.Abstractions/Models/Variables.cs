using System.Collections.Generic;

namespace Elsa.Models
{
    public class Variables
    {
        public Variables()
        {
            Data = new Dictionary<string, object?>();
        }

        public Variables(Variables other) : this(new Dictionary<string, object?>(other.Data))
        {
        }

        public Variables(IDictionary<string, object?> data)
        {
            Data = data;
        }

        public IDictionary<string, object?> Data { get; }

        public object? Get(string name) => Has(name) ? Data[name] : default;
        public T? Get<T>(string name) => !Has(name) ? default : Get(name).ConvertTo<T>();

        public Variables Set(string name, object? value)
        {
            Data[name] = value;
            return this;
        }

        /// <summary>
        /// Removes a variable of the specified name if it is present.
        /// </summary>
        /// <param name="name">The variable name</param>
        /// <returns>A reference to this same <see cref="Variables"/> instance, so calls may be chained.</returns>
        public Variables Remove(string name)
        {
            Data.Remove(name);
            return this;
        }

        /// <summary>
        /// Removes all of the variables from the current instance, clearing it.
        /// </summary>
        /// <returns>A reference to this same <see cref="Variables"/> instance, so calls may be chained.</returns>
        public Variables RemoveAll()
        {
            Data.Clear();
            return this;
        }

        public bool Has(string name) => Data.ContainsKey(name);
    }
}