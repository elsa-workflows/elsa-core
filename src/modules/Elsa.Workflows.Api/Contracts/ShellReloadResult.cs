namespace Elsa.Workflows.Api.Contracts;

internal class ShellReloadResult
{
    public ShellReloadStatus Status { get; init; }
    public string? RequestedShellId { get; init; }
    public DateTimeOffset ReloadedAt { get; init; }
    public IReadOnlyCollection<ShellReloadItemResult> Shells { get; init; } = [];
}