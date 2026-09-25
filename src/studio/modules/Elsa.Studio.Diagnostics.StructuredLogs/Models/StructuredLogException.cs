namespace Elsa.Studio.Diagnostics.StructuredLogs.Models;

/// <summary>
/// Exception details captured with a structured log event.
/// </summary>
public class StructuredLogException
{
    public string? Type { get; set; }
    public string? Message { get; set; }
    public string? StackTrace { get; set; }
}
