using Elsa.Environments.Models;
using JetBrains.Annotations;

namespace Elsa.Environments.Options;

/// <summary>
/// EnvironmentsOptions for providing environments. 
/// </summary>
[PublicAPI]
public class EnvironmentsOptions
{
    /// <summary>
    /// Gets or sets a list of environments.
    /// </summary>
    public ICollection<ServerEnvironment> Environments { get; set; } = new List<ServerEnvironment>();

    /// <summary>
    /// The name of the default environment.
    /// </summary>
    public string? DefaultEnvironmentName { get; set; }
}