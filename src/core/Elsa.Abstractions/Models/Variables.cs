using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Models
{
    public class Variables : Dictionary<string, JToken>
    {
        public static readonly Variables Empty = new Variables();

        public Variables()
        {
        }

        public Variables(Variables other) : this((IEnumerable<KeyValuePair<string, JToken>>)other)
        {
        }

        public Variables(IEnumerable<KeyValuePair<string, JToken>> dictionary)
        {
            foreach (var item in dictionary)
            {
                this[item.Key] = item.Value;
            }
        }

        public JToken GetVariable(string name)
        {
            return ContainsKey(name) ? this[name] : default;
        }

        public object GetVariable(string name, Type type)
        {
            var value = ContainsKey(name) ? this[name] : default;
            return value == null ? default : value.ToObject(type);
        }

        public T GetVariable<T>(string name)
        {
            var value = ContainsKey(name) ? this[name] : default;
            return value == null ? default : value.ToObject<T>();
        }

        public JToken SetVariable(string name, object value)
        {
            return this[name] = JToken.FromObject(value);
        }

        public void SetVariables(Variables variables) => 
            SetVariables((IEnumerable<KeyValuePair<string, JToken>>)variables);

        public void SetVariables(IEnumerable<KeyValuePair<string, JToken>> variables)
        {
            foreach (var variable in variables)
                SetVariable(variable.Key, variable.Value);
        }

        public bool HasVariable(string name)
        {
            return ContainsKey(name);
        }
    }
}