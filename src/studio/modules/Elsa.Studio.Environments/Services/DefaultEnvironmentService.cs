using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Models;

namespace Elsa.Studio.Environments.Services;

/// <summary>
/// Provides the default implementation of the environment service.
/// </summary>
public class DefaultEnvironmentService : IEnvironmentService
{
    /// <summary>
    /// Occurs when the list of environments changes.
    /// </summary>
    public event Action? EnvironmentsChanged;
    /// <summary>
    /// Occurs when the active environment changes.
    /// </summary>
    public event Action? CurrentEnvironmentChanged;

    /// <summary>
    /// Gets the current environment.
    /// </summary>
    public ServerEnvironment? CurrentEnvironment { get; private set; }
    /// <summary>
    /// Gets the collection of available environments.
    /// </summary>
    public IEnumerable<ServerEnvironment> Environments { get; private set; } = new List<ServerEnvironment>();

    /// <summary>
    /// Sets the available environments and optionally selects a default environment.
    /// </summary>
    public void SetEnvironments(IEnumerable<ServerEnvironment> environments, string? defaultEnvironmentName = null)
    {
        var environmentList = environments.ToList();
        Environments = environmentList;

        if (defaultEnvironmentName != null)
            SetCurrentEnvironment(defaultEnvironmentName);
        else if (CurrentEnvironment == null)
            SetCurrentEnvironment(environmentList.FirstOrDefault()?.Name ?? string.Empty);

        EnvironmentsChanged?.Invoke();
    }

    /// <summary>
    /// Sets the current environment.
    /// </summary>
    /// <param name="name">The name.</param>
    public void SetCurrentEnvironment(string name)
    {
        var environments = Environments.ToList();
        var environment = environments.FirstOrDefault(x => x.Name == name);

        if (environment == null || CurrentEnvironment == environment)
            return;

        CurrentEnvironment = environment;
        CurrentEnvironmentChanged?.Invoke();
    }
}