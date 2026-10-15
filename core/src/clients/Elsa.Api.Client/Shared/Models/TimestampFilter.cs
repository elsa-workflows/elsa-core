using Elsa.Api.Client.Shared.Enums;
using JetBrains.Annotations;

namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents a timestamp filter used for filtering data based on a specified timestamp column and operator.
/// </summary>
[UsedImplicitly]
public class TimestampFilter
{
    /// <summary>
    /// Gets or sets the column to filter by.
    /// </summary>
    public string Column { get; set; } = default!;

    /// <summary>
    /// Gets or sets the operator to use for filtering.
    /// </summary>
    public TimestampFilterOperator Operator { get; set; }

    /// <summary>
    /// Gets or sets the timestamp to filter by.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}