using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Elsa.Models
{
    public class Variables : Dictionary<string, Variable>
    {
        public Variables() : base(0, StringComparer.OrdinalIgnoreCase)
        {
        }

        public Variables(Variables other) : this((IEnumerable<KeyValuePair<string, Variable>>) other)
        {
        }

        public Variables(IEnumerable<KeyValuePair<string, Variable>> dictionary)
        {
            foreach (var item in dictionary)
                this[item.Key] = item.Value;
        }
        
        public Variables(IEnumerable<KeyValuePair<string, object>> dictionary)
        {
            foreach (var item in dictionary)
                SetVariable(item.Key, item.Value);
        }

        public object GetVariable(string name)
        {
            return ContainsKey(name) ? this[name]?.Value : default;
        }

        public T GetVariable<T>(string name)
        {
            if (!HasVariable(name))
                return default;

            var value = this[name]?.Value;

            if (value == null)
                return default;

            if (value is T v)
                return v;

            var converter = TypeDescriptor.GetConverter(typeof(T));
            return (T)converter.ConvertFrom(value);
        }

        public Variables SetVariable(string name, object value)
        {
            this[name] = new Variable(value);
            return this;
        }

        public Variables SetVariable(string name, Variable variable)
        {
            this[name] = variable;
            return this;
        }

        public void SetVariables(Variables variables) =>
            SetVariables((IEnumerable<KeyValuePair<string, Variable>>) variables);

        public Variables SetVariables(IEnumerable<KeyValuePair<string, Variable>> variables)
        {
            foreach (var variable in variables)
                SetVariable(variable.Key, variable.Value);

            return this;
        }

        public bool HasVariable(string name) => ContainsKey(name);
    }
}