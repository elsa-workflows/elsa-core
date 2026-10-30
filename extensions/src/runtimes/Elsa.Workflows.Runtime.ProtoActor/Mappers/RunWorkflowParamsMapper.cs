using Elsa.Extensions;
using Elsa.Workflows.Runtime.ProtoActor.Extensions;
using Elsa.Workflows.Options;
using ProtoRunWorkflowInstanceRequest = Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.RunWorkflowInstanceRequest;

namespace Elsa.Workflows.Runtime.ProtoActor.Mappers;

/// <summary>
/// Maps <see cref="ProtoRunWorkflowInstanceRequest"/> to <see cref="RunWorkflowOptions"/>.
/// </summary>
public class RunWorkflowParamsMapper(ActivityHandleMapper activityHandleMapper)
{
    /// <summary>
    /// Maps <see cref="ProtoRunWorkflowInstanceRequest"/> to <see cref="RunWorkflowOptions"/>.
    /// </summary>
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