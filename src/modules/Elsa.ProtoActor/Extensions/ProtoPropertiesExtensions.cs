using Elsa.ProtoActor.ProtoBuf;

namespace Elsa.ProtoActor.Extensions;

internal static class ProtoPropertiesExtensions
{
    public static IDictionary<string, object> Deserialize(this Properties properties) => properties.Data.Deserialize();

    public static Properties SerializeProperties(this IDictionary<string, object> properties)
    {
        var result = new Properties();
        properties.Serialize(result.Data);
        return result;
    }
}