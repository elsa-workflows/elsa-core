namespace Elsa.Shells.Api.Endpoints.Shells;

internal class ShellReloadResponse
{
    public ShellReloadStatus Status { get; init; }
    public string? RequestedShellId { get; init; }
    public DateTimeOffset ReloadedAt { get; init; }
}

internal enum ShellReloadStatus
{
    Completed,
    Failed,
    NotFound
}
