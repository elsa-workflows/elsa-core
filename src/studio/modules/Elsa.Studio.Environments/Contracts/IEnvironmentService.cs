using Elsa.Studio.Environments.Models;

namespace Elsa.Studio.Environments.Contracts;

/// <summary>
/// Manages the environment in which the dashboard is running.
/// </summary>
public interface IEnvironmentService
{
    /// <summary>
    /// Raised when the list of environments changed.
    /// </summary>
    event Action? EnvironmentsChanged;
    
    /// <summary>
    /// Raised when the current environment changed.
    /// </summary>
    event Action CurrentEnvironmentChanged;
    
    /// <summary>
    /// The current environment.
    /// </summary>
    ServerEnvironment? CurrentEnvironment { get; }
    
    /// <summary>
    /// Gets or sets a list of environments.
    /// </summary>
    IEnumerable<ServerEnvironment> Environments { get; }

    /// <summary>
    /// Sets the list of environments.
    /// </summary>
    /// <param name="environments">The environments to set.</param>
    /// <param name="defaultEnvironmentName">The name of the default environment.</param>
    void SetEnvironments(IEnumerable<ServerEnvironment> environments, string? defaultEnvironmentName = null);

    /// <summary>
    /// Sets the current environment.
    /// </summary>
    /// <param name="name">The name of the current environment to set.</param>
    void SetCurrentEnvironment(string name);
}