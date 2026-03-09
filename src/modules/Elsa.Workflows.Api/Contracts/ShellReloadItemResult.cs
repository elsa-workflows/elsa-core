namespace Elsa.Workflows.Api.Contracts;

internal class ShellReloadItemResult
{
    public string ShellId { get; init; } = default!;
    public ShellReloadItemOutcome Outcome { get; init; }
    public bool Requested { get; init; }
    public string? Message { get; init; }
}

internal enum ShellReloadItemOutcome
{
    Reloaded,
    Unchanged,
    Removed,
    InvalidConfiguration,
    Unknown,
    Skipped
}