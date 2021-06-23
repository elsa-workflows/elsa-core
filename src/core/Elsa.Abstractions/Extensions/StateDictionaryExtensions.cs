using System;
using System.Collections.Generic;
using Elsa.Serialization.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa
{
    public static class StateDictionaryExtensions
    {
        public static T? GetState<T>(this IDictionary<string, object?>? state, string key) => state.GetState<T>(key, () => default!);

        public static T GetState<T>(this IDictionary<string, object?>? state, string key, Func<T> defaultValue)
        {
            var item = state?.ContainsKey(key) == true ? state![key] : default;

            if (item == null) 
            {
                if (state != null)
                {
                    state.SetState(key, defaultValue());
                    item = state![key];
                }
                else
                    return defaultValue();
            }

            return item.ConvertTo<T>()!;
        }

        public static object? GetState(this IDictionary<string, object?>? state, string key, Type targetType)
        {
            var item = state.GetState(key);
            return item?.ConvertTo(targetType);
        }
        
        public static object? GetState(this IDictionary<string, object?>? state, string key) => state?.ContainsKey(key) == true ? state![key] : default;

        public static void SetState(this IDictionary<string, object?> state, string key, object? value) => state[key] = value;

        public static bool HasKey(this IDictionary<string, object?> state, string key) => state.ContainsKey(key);

        public static JObject SerializeState(object value) => JObject.FromObject(value, CreateSerializer());
        public static object DeserializeState(JToken value, Type targetType) => value.ToObject(targetType, CreateSerializer())!;

        public static JsonSerializer CreateSerializer()
        {
            var serializer = new JsonSerializer();

            serializer.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
            serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
            serializer.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
            serializer.TypeNameHandling = TypeNameHandling.Auto;
            serializer.Converters.Add(new TypeJsonConverter());

            return serializer;
        }
    }
}