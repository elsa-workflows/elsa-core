using Newtonsoft.Json.Linq;

namespace Elsa.Serialization
{
    public interface ITokenSerializer
    {
        JObject Serialize<T>(T value);
        T Deserialize<T>(JToken token);
        T Deserialize<T>(string json);
    }
}