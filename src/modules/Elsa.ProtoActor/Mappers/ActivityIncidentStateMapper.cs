using Elsa.Workflows.Core.Models;
using ProtoActivityIncident = Elsa.ProtoActor.ProtoBuf.ActivityIncident;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps between <see cref="ActivityIncident"/> and <see cref="ProtoActivityIncident"/>.
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
    /// Maps a <see cref="ProtoActivityIncident"/> to a <see cref="ActivityIncident"/>.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>The mapped <see cref="ActivityIncident"/>.</returns>
    public ActivityIncident Map(ProtoActivityIncident source)
    {
        return new(source.ActivityId, source.ActivityType, source.Message, _exceptionMapper.Map(source.Exception), DateTimeOffset.Parse(source.Timestamp));
    }
    
    /// <summary>
    /// Maps a collection of <see cref="ProtoActivityIncident"/> to a collection of <see cref="ActivityIncident"/>.
    /// </summary>
    public IEnumerable<ActivityIncident> Map(IEnumerable<ProtoActivityIncident> source)
    {
        return source.Select(Map).ToList();
    }

    /// <summary>
    /// Maps a <see cref="ActivityIncident"/> to a <see cref="ProtoActivityIncident"/>.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>The mapped <see cref="ProtoActivityIncident"/>.</returns>
    public ProtoActivityIncident Map(ActivityIncident source)
    {
        return new ProtoActivityIncident
        {
            Exception = source.Exception != null ? _exceptionMapper.Map(source.Exception) : default,
            Message = source.Message,
            ActivityType = source.ActivityType,
            ActivityId = source.ActivityId,
            Timestamp = source.Timestamp.ToString("O")
        };
    }

    /// <summary>
    /// Maps a collection of <see cref="ActivityIncident"/> to a collection of <see cref="ProtoActivityIncident"/>.
    /// </summary>
    public IEnumerable<ProtoActivityIncident> Map(IEnumerable<ActivityIncident> source)
    {
        return source.Select(Map).ToList();
    }
}