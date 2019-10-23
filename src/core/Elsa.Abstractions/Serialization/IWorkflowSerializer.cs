using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization
{
    public interface IWorkflowSerializer
    {
        JsonSerializer Serializer { get; }
        string Serialize<T>(T workflowInstance, string format);
        string Serialize(JToken  token, string format);
        T Deserialize<T>(string data, string format);
        T Deserialize<T>(JToken token);
    }
}