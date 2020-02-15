using Elsa.Serialization;
using Newtonsoft.Json.Linq;

namespace Elsa.Persistence.MongoDb.Serialization
{
    public class JObjectSerializer : JsonSerializerBase<JObject>
    {
        public JObjectSerializer(ITokenSerializer serializer) : base(serializer)
        {
        }
    }
}