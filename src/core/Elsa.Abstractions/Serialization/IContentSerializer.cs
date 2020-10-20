using Newtonsoft.Json.Linq;

namespace Elsa.Serialization
{
    public interface IContentSerializer
    {
        string Serialize<T>(T value);
        T Deserialize<T>(JToken token);
        T Deserialize<T>(string json);
        object GetSettings();
    }
}