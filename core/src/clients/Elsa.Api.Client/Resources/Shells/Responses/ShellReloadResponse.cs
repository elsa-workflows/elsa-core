using Elsa.Api.Client.Resources.Shells.Models;

namespace Elsa.Api.Client.Resources.Shells.Responses;

public class ShellReloadResponse
{
    public ShellReloadStatus Status { get; init; }
    public string? RequestedShellId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string? Message { get; init; }
}