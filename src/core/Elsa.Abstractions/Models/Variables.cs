﻿using System.Collections.Generic;

namespace Elsa.Models
{
    public class Variables : Dictionary<string, object>
    {
        public static readonly Variables Empty = new Variables();

        public Variables()
        {
        }

        public Variables(Variables other) : this((IEnumerable<KeyValuePair<string, object>>) other)
        {
        }

        public Variables(IEnumerable<KeyValuePair<string, object>> dictionary)
        {
            foreach (var item in dictionary)
            {
                this[item.Key] = item.Value;
            }
        }

        public object GetVariable(string name)
        {
            return ContainsKey(name) ? this[name] : null;
        }

        public T GetVariable<T>(string name)
        {
            return ContainsKey(name) ? (T) this[name] : default(T);
        }

        public bool HasVariable(string name, object value)
        {
            return ContainsKey(name) && this[name].Equals(value);
        }

        public bool HasVariable(string name)
        {
            return ContainsKey(name);
        }
    }
}