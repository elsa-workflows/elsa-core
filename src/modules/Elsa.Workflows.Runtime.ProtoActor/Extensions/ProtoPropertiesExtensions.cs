using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;

namespace Elsa.Workflows.Runtime.ProtoActor.Extensions;

public static class ProtoPropertiesExtensions
{
    public static IDictionary<string, object> DeserializeProperties(this Properties properties) => properties.Data.Deserialize();

    public static Properties SerializeProperties(this IDictionary<string, object> properties)
    {
        var result = new Properties();
        properties.Serialize(result.Data);
        return result;
    }
}