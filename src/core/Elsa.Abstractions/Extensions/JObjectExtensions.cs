using System;
using Elsa.Serialization.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa
{
    public static class JObjectExtensions
    {
        public static T? GetState<T>(this JObject? state, string key) => state.GetState<T>(key, () => default!);

        public static T GetState<T>(this JObject? state, string key, Func<T> defaultValue)
        {
            var item = state?.GetValue(key, StringComparison.OrdinalIgnoreCase);

            if (item == null || item.Type == JTokenType.Null)
                return defaultValue();

            return item.ToObject<T>(CreateSerializer())!;
        }
        
        public static object? GetState(this JObject? state, string key, Type targetType)
        {
            var item = state?.GetValue(key, StringComparison.OrdinalIgnoreCase);

            if (item == null || item.Type == JTokenType.Null)
                return null;

            return item.ToObject(targetType, CreateSerializer())!;
        }

        public static T? GetState<T>(this JObject? state, Type type, string key) => state.GetState<T>(type, key, () => default!);

        public static T GetState<T>(this JObject? state, Type type, string key, Func<T> defaultValue)
        {
            var item = state?.GetValue(key, StringComparison.OrdinalIgnoreCase);
            return item != null ? (T) item.ToObject(type, CreateSerializer())! : defaultValue();
        }

        public static void SetState(this JObject state, string key, object? value) => state[key] = value != null ? JToken.FromObject(value, CreateSerializer()) : null;

        public static bool HasKey(this JObject state, string key) => state.ContainsKey(key);
        
        public static JObject CreateState(object value) => JObject.FromObject(value, CreateSerializer());

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