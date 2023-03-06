using Elsa.Common.Contracts;
using Elsa.Elasticsearch.Contracts;
using Humanizer;
using JetBrains.Annotations;

namespace Elsa.Elasticsearch.Strategies;

/// <summary>
/// Returns an index name based on the specified alias, current year and month.
/// </summary>
[PublicAPI]
public class YearAndMonthNaming : IIndexNamingStrategy
{
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Constructor.
    /// </summary>
    public YearAndMonthNaming(ISystemClock systemClock)
    {
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public string GenerateName(string aliasName)
    {
        var now = _systemClock.UtcNow;
        var month = now.ToString("MM");
        var year = now.Year;

        return aliasName.Kebaberize() + "-" + year + "-" + month;
    }
}