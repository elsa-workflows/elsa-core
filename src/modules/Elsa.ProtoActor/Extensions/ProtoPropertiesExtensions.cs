using Elsa.ProtoActor.ProtoBuf;

namespace Elsa.ProtoActor.Extensions;

internal static class ProtoPropertiesExtensions
{
    public static IDictionary<string, object> DeserializeProperties(this ProtoProperties properties) => properties.Data.Deserialize();

    public static ProtoProperties SerializeProperties(this IDictionary<string, object> properties)
    {
        var result = new ProtoProperties();
        properties.Serialize(result.Data);
        return result;
    }
}