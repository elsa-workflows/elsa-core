namespace Elsa.Workflows.Contracts;

/// <summary>
/// Retrieves a name of the current instance.
/// </summary>
public interface IInstanceNameRetriever
{
    /// <summary>
    /// Returns a name for the instance.
    /// </summary>
    public string GetName();
}