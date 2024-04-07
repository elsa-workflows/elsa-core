using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Models;

namespace Elsa.ProtoActor.Mappers;

public class ActivityIncidentMapper(ExceptionMapper exceptionMapper)
{
    public IEnumerable<ActivityIncident> Map(IEnumerable<ProtoActivityIncident> source)
    {
        return source.Select(Map);
    }

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