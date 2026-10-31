namespace Elsa.Hosting.Management.Contracts;

/// <summary>
/// Provides a name of the current application instance.
/// </summary>
public interface IApplicationInstanceNameProvider
{
    /// <summary>
    /// Returns a name for the instance.
    /// </summary>
    public string GetName();
}