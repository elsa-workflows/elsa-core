using System;
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
            var item = state[key];
            return item != null ? item.ToObject<T>(Serializer) : defaultValue != null ? defaultValue() : default;
        }
        
        public static T GetState<T>(this JObject state, Type type, string key, Func<T> defaultValue = null)
        {
            var item = state[key];
            return item != null ? (T) item.ToObject(type, Serializer) : defaultValue != null ? defaultValue() : default;
        }
        
        public static void SetState(this JObject state, string key, object value)
        {
            state[key] = JToken.FromObject(value, Serializer);
        }
    }
}