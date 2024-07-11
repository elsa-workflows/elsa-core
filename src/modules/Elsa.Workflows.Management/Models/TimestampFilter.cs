using Elsa.Workflows.Management.Enums;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Models;

/// <summary>
/// Represents a timestamp filter used for filtering data based on a specified timestamp column and operator.
/// </summary>
[UsedImplicitly]
public class TimestampFilter
{
    /// <summary>
    /// Gets or sets the column to filter by.
    /// </summary>
    public required string Column { get; set; } = default!;

    /// <summary>
    /// Gets or sets the operator to use for filtering.
    /// </summary>
    public required TimestampFilterOperator Operator { get; set; }

    /// <summary>
    /// Gets or sets the timestamp to filter by.
    /// </summary>
    public required DateTimeOffset Timestamp { get; set; }
}