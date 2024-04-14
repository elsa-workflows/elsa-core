using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Runtime.Distributed.Messages;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps <see cref="RunWorkflowParams"/> to <see cref="ProtoRunWorkflowInstanceRequest"/>.
/// </summary>
public class RunWorkflowParamsMapper(ActivityHandleMapper activityHandleMapper)
{
    /// <summary>
    /// Maps <see cref="RunWorkflowParams"/> to <see cref="ProtoRunWorkflowInstanceRequest"/>.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public RunWorkflowParams Map(ProtoRunWorkflowInstanceRequest source)
    {
        return new()
        {
            ActivityHandle = activityHandleMapper.Map(source.ActivityHandle),
            BookmarkId = source.BookmarkId,
            TriggerActivityId = source.TriggerActivityId,
            Input = source.Input?.DeserializeInput(),
            Properties = source.Properties?.DeserializeProperties(),
        };
    }
}