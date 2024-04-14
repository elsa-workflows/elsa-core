using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Runtime.Distributed.Messages;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps <see cref="RunWorkflowInstanceRequest"/> to <see cref="ProtoRunWorkflowInstanceRequest"/> and vice versa.
/// </summary>
public class RunWorkflowInstanceRequestMapper(ActivityHandleMapper activityHandleMapper)
{
    /// <summary>
    /// Maps <see cref="RunWorkflowParams"/> to <see cref="ProtoRunWorkflowInstanceRequest"/>.
    /// </summary>
    public ProtoRunWorkflowInstanceRequest Map(RunWorkflowInstanceRequest? source)
    {
        return new()
        {
            ActivityHandle = activityHandleMapper.Map(source?.ActivityHandle) ?? new(),
            BookmarkId = source?.BookmarkId.EmptyIfNull(),
            TriggerActivityId = source?.TriggerActivityId.EmptyIfNull(),
            Input = source?.Input?.SerializeInput(),
            Properties = source?.Properties?.SerializeProperties(),
        };
    }

    /// <summary>
    /// Maps <see cref="ProtoRunWorkflowInstanceRequest"/> to <see cref="RunWorkflowInstanceRequest"/>.
    /// </summary>
    public RunWorkflowInstanceRequest Map(ProtoRunWorkflowInstanceRequest source)
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