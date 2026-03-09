namespace Elsa.Workflows.Api.Contracts;

internal interface IShellReloadOrchestrator
{
    Task<ShellReloadResult> ReloadAllAsync(CancellationToken cancellationToken = default);
    Task<ShellReloadResult> ReloadAsync(string shellId, CancellationToken cancellationToken = default);
}