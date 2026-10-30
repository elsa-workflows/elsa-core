using Elsa.Api.Client.Shared.Enums;
using JetBrains.Annotations;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceList.Models;

/// <summary>
/// Represents a timestamp filter used for filtering data based on a specified timestamp column and operator.
/// </summary>
public class TimestampFilterModel
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
    /// Gets or sets the date to filter by.
    /// </summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time to filter by.
    /// </summary>
    public string? Time { get; set; }
}
