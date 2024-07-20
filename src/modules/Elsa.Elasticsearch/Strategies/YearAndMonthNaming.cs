using Elsa.Common.Contracts;
using Elsa.Elasticsearch.Contracts;
using Humanizer;
using JetBrains.Annotations;

namespace Elsa.Elasticsearch.Strategies;

/// <summary>
/// Returns an index name based on the specified alias, current year and month.
/// </summary>
/// <remarks>
/// Constructor.
/// </remarks>
[PublicAPI]
public class YearAndMonthNaming(ISystemClock systemClock) : IIndexNamingStrategy
{
    private readonly ISystemClock _systemClock = systemClock;

    /// <inheritdoc />
    public string GenerateName(string aliasName)
    {
        var now = _systemClock.UtcNow;
        var month = now.ToString("MM");
        var year = now.Year;

        return aliasName.Kebaberize() + "-" + year + "-" + month;
    }
}