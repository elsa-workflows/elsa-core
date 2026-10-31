namespace Elsa.Diagnostics.StructuredLogs.Models;

public record StructuredLogException(
    string Type,
    string Message,
    string? StackTrace);
