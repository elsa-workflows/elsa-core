using System;
using Jint;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Extensions
{
    public static class StateExtensions
    {
        private static readonly JsonSerializer Serializer = new JsonSerializer().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        
        public static T GetState<T>(this JObject state, string key, Func<T> defaultValue = null)
        {
            var item = state.GetValue(key, StringComparison.OrdinalIgnoreCase);
            return item != null && item.Type != JTokenType.Null ? item.ToObject<T>(Serializer) : defaultValue != null ? defaultValue() : default(T);
        }
        
        public static T GetState<T>(this JObject state, Type type, string key, Func<T> defaultValue = null)
        {
            var item = state.GetValue(key, StringComparison.OrdinalIgnoreCase);
            return item != null ? (T) item.ToObject(type, Serializer) : defaultValue != null ? defaultValue() : default;
        }
        
        public static void SetState(this JObject state, string key, object value)
        {
            state[key] = value != null 
                ? JToken.FromObject(value, Serializer)
                : null;
        }
    }
}