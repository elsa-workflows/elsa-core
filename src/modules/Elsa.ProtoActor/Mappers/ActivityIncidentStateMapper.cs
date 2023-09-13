using Elsa.Workflows.Core.State;
using ProtoActivityIncident = Elsa.ProtoActor.ProtoBuf.ActivityIncident;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps between <see cref="ActivityIncidentState"/> and <see cref="ProtoActivityIncident"/>.
/// </summary>
internal class ActivityIncidentStateMapper
{
    private readonly ExceptionMapper _exceptionMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityIncidentStateMapper"/> class.
    /// </summary>
    public ActivityIncidentStateMapper(ExceptionMapper exceptionMapper)
    {
        _exceptionMapper = exceptionMapper;
    }

    /// <summary>
    /// Maps a <see cref="ProtoActivityIncident"/> to a <see cref="ActivityIncidentState"/>.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>The mapped <see cref="ActivityIncidentState"/>.</returns>
    public ActivityIncidentState Map(ProtoActivityIncident source)
    {
        return new(source.ActivityId, source.ActivityType, source.Message, _exceptionMapper.Map(source.Exception));
    }
    
    /// <summary>
    /// Maps a collection of <see cref="ProtoActivityIncident"/> to a collection of <see cref="ActivityIncidentState"/>.
    /// </summary>
    public IEnumerable<ActivityIncidentState> Map(IEnumerable<ProtoActivityIncident> source)
    {
        return source.Select(Map).ToList();
    }

    /// <summary>
    /// Maps a <see cref="ActivityIncidentState"/> to a <see cref="ProtoActivityIncident"/>.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>The mapped <see cref="ProtoActivityIncident"/>.</returns>
    public ProtoActivityIncident Map(ActivityIncidentState source)
    {
        return new ProtoActivityIncident
        {
            Exception = source.Exception != null ? _exceptionMapper.Map(source.Exception) : default,
            Message = source.Message,
            ActivityType = source.ActivityType,
            ActivityId = source.ActivityId,
        };
    }

    /// <summary>
    /// Maps a collection of <see cref="ActivityIncidentState"/> to a collection of <see cref="ProtoActivityIncident"/>.
    /// </summary>
    public IEnumerable<ProtoActivityIncident> Map(IEnumerable<ActivityIncidentState> source)
    {
        return source.Select(Map).ToList();
    }
}