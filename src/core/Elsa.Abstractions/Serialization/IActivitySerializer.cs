using Elsa.Services;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization
{
    public interface IActivitySerializer
    {
        JObject Serialize<T>(T activity) where T : IActivity;
        T Deserialize<T>(JObject data) where T : IActivity;
    }
}