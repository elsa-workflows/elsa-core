using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Abstractions;
using Elsa.Alterations.Core.Contexts;
using JetBrains.Annotations;

namespace Elsa.Alterations.AlterationHandlers;

/// <summary>
/// Upgrades the version of the workflow instance.
/// </summary>
[UsedImplicitly]
public class CancelHandler : AlterationHandlerBase<Cancel>
{
    /// <inheritdoc />
    protected override ValueTask HandleAsync(AlterationContext context, Cancel alteration)
    {
        context.WorkflowExecutionContext.Cancel();
        
        context.Succeed();
        return ValueTask.CompletedTask;
    }
}