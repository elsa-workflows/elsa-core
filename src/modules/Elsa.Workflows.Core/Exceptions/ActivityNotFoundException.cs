namespace Elsa.Workflows.Exceptions;

/// <summary>
/// Thrown when a NotFoundActivity is executed.
/// </summary>
public class ActivityNotFoundException : Exception
{
    /// <inheritdoc />
    public ActivityNotFoundException(string missingTypeName) : base($"Activity type '{missingTypeName}' could not be found.")
    {
        MissingTypeName = missingTypeName;
    }
    
    /// <inheritdoc />
    public ActivityNotFoundException(string missingTypeName, int missingTypeVersion) : base($"Activity type '{missingTypeName}' version '{missingTypeVersion}' could not be found.")
    {
        MissingTypeName = missingTypeName;
        MissingTypeVersion = missingTypeVersion;
    }
    
    /// <summary>
    /// The type name of the missing activity type.
    /// </summary>
    public string MissingTypeName { get; }
    
    /// <summary>
    /// The version of the missing activity type.
    /// </summary>
    public int MissingTypeVersion { get; }
}