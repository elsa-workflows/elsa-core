using Elsa.Common.Multitenancy;
using Elsa.Workflows.Runtime.StartupTasks;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Runs <see cref="RecoverInterruptedWorkflowsStartupTask"/> once all tenants are activated, in a new scope and through
/// <see cref="ITaskExecutor"/> so that its single-node lock applies.
/// </summary>
internal sealed class RecoverInterruptedWorkflowsOnAllTenantsActivated(IServiceScopeFactory scopeFactory) : IAllTenantsActivatedEvent
{
    /// <inheritdoc />
    public async Task AllTenantsActivatedAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var task = ActivatorUtilities.CreateInstance<RecoverInterruptedWorkflowsStartupTask>(scope.ServiceProvider);
        var taskExecutor = scope.ServiceProvider.GetRequiredService<ITaskExecutor>();
        await taskExecutor.ExecuteTaskAsync(task, cancellationToken);
    }
}
