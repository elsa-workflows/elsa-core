using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;

namespace Elsa.Workflows.Runtime.ProtoActor.Extensions;

public static class ProtoPropertiesExtensions
{
    public static IDictionary<string, object> DeserializeProperties(this ProtoProperties properties) => properties.Data.Deserialize();

    public static ProtoProperties SerializeProperties(this IDictionary<string, object> properties)
    {
        var result = new ProtoProperties();
        properties.Serialize(result.Data);
        return result;
    }
}