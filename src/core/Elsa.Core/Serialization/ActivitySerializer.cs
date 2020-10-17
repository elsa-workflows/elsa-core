using Elsa.Services;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization
{
    public class ActivitySerializer : IActivitySerializer
    {
        private readonly IJsonSerializer _ijSerializer;
        public ActivitySerializer(IJsonSerializer ijSerializer) => _ijSerializer = ijSerializer;
        public JObject Serialize<T>(T activity) where T : IActivity => _ijSerializer.Serialize(activity);
        public T Deserialize<T>(JObject data) where T : IActivity => _ijSerializer.Deserialize<T>(data);
    }
}