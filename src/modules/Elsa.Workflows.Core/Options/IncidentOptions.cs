namespace Elsa.Workflows.Core.Options;

/// <summary>
/// Options for configuring how incidents are handled.
/// </summary>
public class IncidentOptions
{
    /// <summary>
    /// Gets or sets the default incident strategy to use when no strategy is configured on the workflow.
    /// </summary>
    public Type? DefaultIncidentStrategy { get; set; }
}