using Elsa.Workflows.Runtime.ProtoActor.Extensions;
using Elsa.Workflows.Options;
using ProtoRunWorkflowInstanceRequest = Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.RunWorkflowInstanceRequest;

namespace Elsa.Workflows.Runtime.ProtoActor.Mappers;

/// Maps <see cref="ProtoRunWorkflowInstanceRequest"/> to <see cref="RunWorkflowOptions"/>.
public class RunWorkflowParamsMapper(ActivityHandleMapper activityHandleMapper)
{
    /// Maps <see cref="ProtoRunWorkflowInstanceRequest"/> to <see cref="RunWorkflowOptions"/>.
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