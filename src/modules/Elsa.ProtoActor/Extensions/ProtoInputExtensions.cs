using Elsa.ProtoActor.ProtoBuf;

namespace Elsa.ProtoActor.Extensions;

internal static class ProtoInputExtensions
{
    public static IDictionary<string, object> DeserializeInput(this ProtoInput input) => input.Data.Deserialize();

    public static ProtoInput SerializeInput(this IDictionary<string, object> input)
    {
        var result = new ProtoInput();
        input.Serialize(result.Data);
        return result;
    }
}