using Elsa.ProtoActor.ProtoBuf;

namespace Elsa.ProtoActor.Extensions;

internal static class ProtoOutputExtensions
{
    public static IDictionary<string, object> Deserialize(this Output output) => output.Data.Deserialize();

    public static Output SerializeOutput(this IDictionary<string, object> output)
    {
        var result = new Output();
        output.Serialize(result.Data);
        return result;
    }
}