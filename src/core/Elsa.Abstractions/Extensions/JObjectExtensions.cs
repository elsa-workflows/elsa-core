using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa
{
    public static class JObjectExtensions
    {
        private static readonly JsonSerializer Serializer = new JsonSerializer().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

        public static T? GetState<T>(this JObject? state, string key, Func<T>? defaultValue = null)
        {
            var item = state?.GetValue(key, StringComparison.OrdinalIgnoreCase);

            if (item == null || item.Type == JTokenType.Null)
                return defaultValue != null ? defaultValue() : default;

            return item.ToObject<T>(Serializer)!;
        }

        public static T? GetState<T>(this JObject? state, Type type, string key, Func<T>? defaultValue = null)
        {
            var item = state?.GetValue(key, StringComparison.OrdinalIgnoreCase);
            return item != null ? (T)item.ToObject(type, Serializer)! : defaultValue != null ? defaultValue() : default;
        }

        public static void SetState(this JObject state, string key, object? value) => state[key] = value != null ? JToken.FromObject(value, Serializer) : null;
        
        public static bool HasKey(this JObject state, string key) => state.ContainsKey(key);
    }
}