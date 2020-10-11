using Elsa.Services;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization
{
    public class ActivitySerializer : IActivitySerializer
    {
        private readonly ITokenSerializer _tokenSerializer;
        public ActivitySerializer(ITokenSerializer tokenSerializer) => _tokenSerializer = tokenSerializer;
        public JObject Serialize<T>(T activity) where T : IActivity => _tokenSerializer.Serialize(activity);
        public T Deserialize<T>(JObject data) where T : IActivity => _tokenSerializer.Deserialize<T>(data);
    }
}