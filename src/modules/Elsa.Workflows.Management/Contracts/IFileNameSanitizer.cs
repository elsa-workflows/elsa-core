namespace Elsa.Workflows.Management;

/// <summary>
/// Sanitizes file names by replacing invalid characters with safe alternatives.
/// </summary>
public interface IFileNameSanitizer
{
    /// <summary>
    /// Replaces invalid file name characters in the specified value.
    /// </summary>
    string Sanitize(string value);
}

