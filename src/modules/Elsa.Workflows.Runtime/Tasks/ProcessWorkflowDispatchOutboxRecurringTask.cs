using Elsa.Common;
using Elsa.Common.RecurringTasks;
using Elsa.Workflows.Runtime.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Tasks;

/// <summary>
/// Periodically processes pending workflow dispatch outbox items.
/// </summary>
[SingleNodeTask]
[UsedImplicitly]
public class ProcessWorkflowDispatchOutboxRecurringTask(IWorkflowDispatchOutboxProcessor processor, IOptions<WorkflowDispatcherOptions> options) : RecurringTask
{
    /// <inheritdoc />
    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.UseTransactionalOutbox)
            return;

        await processor.ProcessAsync(cancellationToken);
    }
}
