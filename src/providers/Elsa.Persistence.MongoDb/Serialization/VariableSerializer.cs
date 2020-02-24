using Elsa.Models;
using Elsa.Serialization;

namespace Elsa.Persistence.MongoDb.Serialization
{
    public class VariableSerializer : JsonSerializerBase<Variable>
    {
        public VariableSerializer(ITokenSerializer serializer) : base(serializer)
        {
        }
    }
}