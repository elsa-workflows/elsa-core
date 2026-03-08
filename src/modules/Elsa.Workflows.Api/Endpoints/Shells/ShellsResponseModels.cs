using Elsa.Workflows.Api.Contracts;

namespace Elsa.Workflows.Api.Endpoints.Shells;

internal class ShellReloadResponse
{
    public ShellReloadStatus Status { get; init; }
    public string? RequestedShellId { get; init; }
    public DateTimeOffset ReloadedAt { get; init; }
    public IReadOnlyCollection<ShellReloadItemResponse> Shells { get; init; } = [];

    public static ShellReloadResponse FromResult(ShellReloadResult result) => new()
    {
        Status = result.Status,
        RequestedShellId = result.RequestedShellId,
        ReloadedAt = result.ReloadedAt,
        Shells = result.Shells.Select(x => new ShellReloadItemResponse
        {
            ShellId = x.ShellId,
            Outcome = x.Outcome,
            Requested = x.Requested,
            Message = x.Message
        }).ToArray()
    };
}

internal class ShellReloadItemResponse
{
    public string ShellId { get; init; } = default!;
    public ShellReloadItemOutcome Outcome { get; init; }
    public bool Requested { get; init; }
    public string? Message { get; init; }
}
