using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Models;

namespace Elsa.ProtoActor.Mappers;

public class ActivityHandleMapper
{
    public ProtoActivityHandle? Map(ActivityHandle? source)
    {
        if (source == null)
            return null;

        return new()
        {
            ActivityId = source.ActivityId,
            ActivityHash = source.ActivityHash,
            ActivityInstanceId = source.ActivityInstanceId,
            ActivityNodeId = source.ActivityNodeId,
        };
    }
}