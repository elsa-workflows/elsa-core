namespace Elsa.Studio;

/// <summary>
/// Represents the version of the tool.
/// </summary>
public static class ToolVersion
{
    /// <summary>
    /// The version of the tool.
    /// </summary>
    public static readonly Version Version = new(3, 8, 0, 0);
    
    /// <summary>
    /// Gets the display version of the tool.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public static string GetDisplayVersion() => Version.ToString(2);
}