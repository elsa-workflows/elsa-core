namespace Elsa.Workflows.Api.Contracts;

internal enum ShellReloadStatus
{
    Completed,
    Partial,
    Failed,
    Busy,
    NotFound,
    RequestedShellFailed
}