namespace Elsa.ServerLogs.Models;

public record ServerLogException(
    string Type,
    string Message,
    string? StackTrace);
