using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;

namespace Elsa.Workflows.Runtime.ProtoActor.Extensions;

internal static class ProtoOutputExtensions
{
    public static IDictionary<string, object> DeserializeOutput(this Output output) => output.Data.Deserialize();

    public static Output SerializeOutput(this IDictionary<string, object> output)
    {
        var result = new Output();
        output.Serialize(result.Data);
        return result;
    }
}
