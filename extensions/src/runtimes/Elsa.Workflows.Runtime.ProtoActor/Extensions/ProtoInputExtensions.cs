using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;

namespace Elsa.Workflows.Runtime.ProtoActor.Extensions;

internal static class ProtoInputExtensions
{
    public static IDictionary<string, object> DeserializeInput(this Input input) => input.Data.Deserialize();

    public static Input SerializeInput(this IDictionary<string, object> input)
    {
        var result = new Input();
        input.Serialize(result.Data);
        return result;
    }
}