using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Options;
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