using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Models
{
    public class Variables : IDictionary<string, JToken>
    {
        public static readonly Variables Empty = new Variables();
        private ConcurrentDictionary<string, object> objects = new ConcurrentDictionary<string, object>();

        public ICollection<string> Keys => objects.Keys;

        public ICollection<JToken> Values
        {
            get
            {
                return objects.Values.Select(o => GetJToken(o)).ToList();
            }
        }

        public int Count => objects.Count;

        public bool IsReadOnly => false;


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

        public JToken this[string key]
        {
            get
            {
                return GetJToken(key);
            }
            set
            {
                Set(key, value);
            }
        }

        private void Set(string key, object value)
        {
            objects.TryAdd(key, value);
        }

        private JToken GetJToken(string key)
        {
            object value;
            JToken token;
            objects.TryGetValue(key, out value);

            if (value != null && value.GetType() != typeof(JToken))
                token = JToken.FromObject(value);
            else
                token = (JToken)value;

            return token;
        }

        private JToken GetJToken(object value)
        {
            JToken token;
           
            if (value != null && value.GetType() != typeof(JToken))
                token = JToken.FromObject(value);
            else
                token = (JToken)value;

            return token;
        }

        public bool ContainsKey(string key)
        {
            return objects.ContainsKey(key);
        }

        public JToken GetVariable(string name)
        {
            return ContainsKey(name) ? this[name] : default;
        }

        public object GetVariable(string name, Type type)
        {
            object value;
            objects.TryGetValue(name, out value);
            return value == null ? default : value;
        }

        public T GetVariable<T>(string name)
        {
            object value;
            objects.TryGetValue(name, out value);
            return value == null ? default : (T)value;
        }

        public JToken SetVariable(string name, object value)
        {
            objects.TryAdd(name, value);
            return GetJToken(value);
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

        public void Add(string key, JToken value)
        {
            Set(key, value);
        }

        bool IDictionary<string, JToken>.ContainsKey(string key)
        {
            return objects.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            object outRemove;
            return objects.TryRemove(key, out outRemove);
        }

        public bool TryGetValue(string key, out JToken value)
        {
            object valueObject;
            bool found = objects.TryGetValue(key, out valueObject);

            if (found)
                value = GetJToken(valueObject);
            else
                value = null;

            return found;
        }

        public void Add(KeyValuePair<string, JToken> item)
        {
            objects.TryAdd(item.Key, item.Value);
        }

        public void Clear()
        {
            objects.Clear();
        }

        public bool Contains(KeyValuePair<string, JToken> item)
        {
            KeyValuePair<string, object> searchItem = new KeyValuePair<string, object>(item.Key, item.Value);
            return objects.Contains(searchItem);
        }

        public void CopyTo(KeyValuePair<string, JToken>[] array, int arrayIndex)
        {
            var newArray =
            objects.Select(
                kv =>
                new KeyValuePair<string, JToken>(kv.Key, GetJToken(kv.Key))
                ).ToArray();

            newArray.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, JToken> item)
        {
            object removeObject;
            return objects.TryRemove(item.Key, out removeObject);
        }

        public IEnumerator<KeyValuePair<string, JToken>> GetEnumerator()
        {
            return objects.Select(
                  kv =>
                  new KeyValuePair<string, JToken>(kv.Key, GetJToken(kv.Key))
                  ).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}