using Elsa.Environments.Models;

namespace Elsa.Environments.Options;

/// <summary>
/// EnvironmentsOptions for providing environments. 
/// </summary>
public class EnvironmentsOptions
{
    /// <summary>
    /// Gets or sets a list of environments.
    /// </summary>
    public ICollection<WorkflowsEnvironment> Environments { get; set; } = new List<WorkflowsEnvironment>();
}