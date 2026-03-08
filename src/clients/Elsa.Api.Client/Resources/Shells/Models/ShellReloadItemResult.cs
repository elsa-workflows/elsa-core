namespace Elsa.Api.Client.Resources.Shells.Models;

public class ShellReloadItemResult
{
    public string ShellId { get; init; } = default!;
    public ShellReloadItemOutcome Outcome { get; init; }
    public bool Requested { get; init; }
    public string? Message { get; init; }
}

public enum ShellReloadItemOutcome
{
    Reloaded,
    Unchanged,
    Removed,
    InvalidConfiguration,
    Unknown,
    Skipped
}