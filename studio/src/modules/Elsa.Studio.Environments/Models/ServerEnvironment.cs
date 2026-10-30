namespace Elsa.Studio.Environments.Models;

/// <summary>
/// Represents the environment in which the workflow engine is running.
/// </summary>
public class ServerEnvironment
{
    /// <summary>
    /// The name of the environment.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// The URL of the server hosting the workflow engine.
    /// </summary>
    public Uri Url { get; set; } = default!;

    /// <summary>
    /// A dictionary of custom properties.
    /// </summary>
    public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
}