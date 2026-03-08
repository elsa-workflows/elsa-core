using Elsa.Workflows.Api.Contracts;

namespace Elsa.Workflows.Api.Endpoints.Shells.Reload;

internal class Request
{
    public string ShellId { get; set; } = null!;
}

internal class Response
{
    public ShellReloadStatus Status { get; init; }
    public string? RequestedShellId { get; init; }
    public DateTimeOffset ReloadedAt { get; init; }
    public IReadOnlyCollection<ShellResponse> Shells { get; init; } = [];

    public static Response FromResult(ShellReloadResult result) => new()
    {
        Status = result.Status,
        RequestedShellId = result.RequestedShellId,
        ReloadedAt = result.ReloadedAt,
        Shells = result.Shells.Select(x => new ShellResponse
        {
            ShellId = x.ShellId,
            Outcome = x.Outcome,
            Requested = x.Requested,
            Message = x.Message
        }).ToArray()
    };
}

internal class ShellResponse
{
    public string ShellId { get; init; } = null!;
    public ShellReloadItemOutcome Outcome { get; init; }
    public bool Requested { get; init; }
    public string? Message { get; init; }
}