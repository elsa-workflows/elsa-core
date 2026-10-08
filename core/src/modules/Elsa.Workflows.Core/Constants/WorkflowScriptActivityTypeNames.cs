namespace Elsa.Workflows;

/// <summary>
/// Activity type names for built-in workflow script activities.
/// </summary>
public static class WorkflowScriptActivityTypeNames
{
    /// <summary>
    /// The Elsa activity namespace for built-in script activities.
    /// </summary>
    public const string Namespace = "Elsa";

    /// <summary>
    /// The unqualified C# script activity type.
    /// </summary>
    public const string RunCSharpType = "RunCSharp";

    /// <summary>
    /// The fully qualified C# script activity type name.
    /// </summary>
    public const string RunCSharp = $"{Namespace}.{RunCSharpType}";

    /// <summary>
    /// The unqualified Python script activity type.
    /// </summary>
    public const string RunPythonType = "RunPython";

    /// <summary>
    /// The fully qualified Python script activity type name.
    /// </summary>
    public const string RunPython = $"{Namespace}.{RunPythonType}";
}
