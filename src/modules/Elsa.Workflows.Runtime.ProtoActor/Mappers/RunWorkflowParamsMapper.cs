using Elsa.Workflows.Runtime.ProtoActor.Extensions;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;

namespace Elsa.Workflows.Runtime.ProtoActor.Mappers;

/// Maps <see cref="RunWorkflowParams"/> to <see cref="ProtoRunWorkflowInstanceRequest"/>.
public class RunWorkflowParamsMapper(ActivityHandleMapper activityHandleMapper)
{
    /// Maps <see cref="RunWorkflowParams"/> to <see cref="ProtoRunWorkflowInstanceRequest"/>.
    public RunWorkflowOptions Map(ProtoRunWorkflowInstanceRequest source)
    {
        return new()
        {
            ActivityHandle = activityHandleMapper.Map(source.ActivityHandle),
            BookmarkId = source.BookmarkId?.NullIfEmpty(),
            TriggerActivityId = source.TriggerActivityId?.NullIfEmpty(),
            Input = source.Input?.DeserializeInput(),
            Properties = source.Properties?.DeserializeProperties()
        };
    }
}