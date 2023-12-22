using Elsa.ProtoActor.ProtoBuf;

namespace Elsa.ProtoActor.Extensions;

internal static class ProtoInputExtensions
{
    public static IDictionary<string, object> Deserialize(this Input input) => input.Data.Deserialize();

    public static Input SerializeInput(this IDictionary<string, object> input)
    {
        var result = new Input();
        input.Serialize(result.Data);
        return result;
    }
}