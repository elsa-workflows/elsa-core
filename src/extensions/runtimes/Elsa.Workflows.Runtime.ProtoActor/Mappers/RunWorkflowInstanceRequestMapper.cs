using Elsa.Extensions;
using Elsa.Workflows.Runtime.ProtoActor.Extensions;
using Elsa.Workflows.Runtime.Messages;
using ProtoRunWorkflowInstanceRequest = Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.RunWorkflowInstanceRequest;

namespace Elsa.Workflows.Runtime.ProtoActor.Mappers;

/// <summary>
/// Maps <see cref="RunWorkflowInstanceRequest"/> to <see cref="ProtoRunWorkflowInstanceRequest"/> and vice versa.
/// </summary>
public class RunWorkflowInstanceRequestMapper(ActivityHandleMapper activityHandleMapper)
{
    /// <summary>
    /// Maps <see cref="RunWorkflowInstanceRequest"/> to <see cref="ProtoRunWorkflowInstanceRequest"/>.
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
            IncludeWorkflowOutput = source?.IncludeWorkflowOutput ?? false,
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
            IncludeWorkflowOutput = source.IncludeWorkflowOutput,
        };
    }
}