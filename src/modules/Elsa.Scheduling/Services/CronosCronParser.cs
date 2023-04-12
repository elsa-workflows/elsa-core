using Cronos;
using Elsa.Common.Contracts;
using Elsa.Scheduling.Contracts;

namespace Elsa.Scheduling.Services;

/// <summary>
/// A cron parser that uses Cronos.
/// </summary>
public class CronosCronParser : ICronParser
{
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Initializes a new instance of the <see cref="CronosCronParser"/> class.
    /// </summary>
    public CronosCronParser(ISystemClock systemClock)
    {
        _systemClock = systemClock;
    }
    
    /// <inheritdoc />
    public DateTimeOffset GetNextOccurrence(string expression)
    {
        var parsedExpression = CronExpression.Parse(expression, CronFormat.IncludeSeconds);
        var now = _systemClock.UtcNow;
        return parsedExpression.GetNextOccurrence(now, TimeZoneInfo.Utc).GetValueOrDefault();
    }
}