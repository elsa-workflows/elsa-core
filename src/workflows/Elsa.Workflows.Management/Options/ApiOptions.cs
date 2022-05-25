namespace Elsa.Workflows.Management.Options;

public class ApiOptions
{
    /// <summary>
    /// A collection of activity types made available from the API.
    /// </summary>
    public HashSet<Type> ActivityTypes { get; set; } = new();
}