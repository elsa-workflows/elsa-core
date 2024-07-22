using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.ProtoActor.Mappers;

/// <summary>
/// Maps <see cref="ActivityIncident"/> to <see cref="ProtoActivityIncident"/>.
/// </summary>
public class ActivityIncidentMapper(ExceptionMapper exceptionMapper)
{
    /// <summary>
    /// Maps <see cref="ActivityIncident"/> to <see cref="ProtoActivityIncident"/>.
    /// </summary>
    public IEnumerable<ActivityIncident> Map(IEnumerable<ProtoActivityIncident> source)
    {
        return source.Select(Map);
    }
    
    /// <summary>
    /// Maps <see cref="ActivityIncident"/> to <see cref="ProtoActivityIncident"/>.
    /// </summary>
    public IEnumerable<ProtoActivityIncident> Map(IEnumerable<ActivityIncident> source)
    {
        return source.Select(Map);
    }

    /// <summary>
    /// Maps <see cref="ActivityIncident"/> to <see cref="ProtoActivityIncident"/>.
    /// </summary>
    public ProtoActivityIncident Map(ActivityIncident source)
    {
        return new()
        {
            ActivityId = source.ActivityId,
            ActivityType = source.ActivityType,
            Message = source.Message,
            Exception = exceptionMapper.Map(source.Exception),
            Timestamp = source.Timestamp.ToString()
        };
    }

    /// <summary>
    /// Maps <see cref="ProtoActivityIncident"/> to <see cref="ActivityIncident"/>.
    /// </summary>
    public ActivityIncident Map(ProtoActivityIncident source)
    {
        return new()
        {
            ActivityId = source.ActivityId,
            ActivityType = source.ActivityType,
            Message = source.Message,
            Exception = exceptionMapper.Map(source.Exception),
            Timestamp = DateTime.Parse(source.Timestamp)
        };
    }
}