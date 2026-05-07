namespace Elsa.Diagnostics.Models;

public record ServerLogException(
    string Type,
    string Message,
    string? StackTrace);
