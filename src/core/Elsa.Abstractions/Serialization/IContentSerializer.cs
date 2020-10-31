using System;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization
{
    public interface IContentSerializer
    {
        string Serialize<T>(T value);
        T Deserialize<T>(JToken token);
        object? Deserialize(JToken token, Type targetType);
        T Deserialize<T>(string json);
        object? Deserialize(string json, Type targetType);
        object GetSettings();
    }
}