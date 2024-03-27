using Elsa.Alterations.Core.Contexts;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Middleware.Workflows;
using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Alterations.Middleware.Workflows;

public static class UseRunAlterationsMiddlewareExtensions
{
    /// <summary>
    /// Installs middleware that executes scheduled work items. 
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseAlterationsRunnerMiddleware(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<RunAlterationsMiddleware>();
}

public class RunAlterationsMiddleware : WorkflowExecutionMiddleware
{
    private readonly IEnumerable<IAlterationHandler> _handlers;
    public static readonly object AlterationsPropertyKey = new();
    public static readonly object AlterationsLogPropertyKey = new();

    public RunAlterationsMiddleware(WorkflowMiddlewareDelegate next, IEnumerable<IAlterationHandler> handlers) : base(next)
    {
        _handlers = handlers;
    }

    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var alterations = (IEnumerable<IAlteration>)(context.TransientProperties.GetValue(AlterationsPropertyKey) ?? throw new InvalidOperationException("No alterations found in the transient properties."));
        var log = (AlterationLog)(context.TransientProperties.GetValue(AlterationsLogPropertyKey) ?? throw new InvalidOperationException("No alteration log found in the transient properties."));
        await RunAsync(context, alterations, log, context.CancellationTokens.ApplicationCancellationToken);
    }

    private async Task<bool> RunAsync(WorkflowExecutionContext workflowExecutionContext, IEnumerable<IAlteration> alterations, AlterationLog log, CancellationToken cancellationToken = default)
    {
        var commitActions = new List<Func<Task>>();

        foreach (var alteration in alterations)
        {
            // Find handlers.
            var handlers = _handlers.Where(x => x.CanHandle(alteration)).ToList();

            foreach (var handler in handlers)
            {
                // Execute handler.
                var alterationContext = new AlterationContext(alteration, workflowExecutionContext, log, cancellationToken);
                await handler.HandleAsync(alterationContext);

                // If the handler has failed, exit.
                if (alterationContext.HasFailed)
                    return false;

                // Collect the commit handler, if any.
                if (alterationContext.CommitAction != null)
                    commitActions.Add(alterationContext.CommitAction);
            }
        }

        // Execute commit handlers.
        foreach (var commitAction in commitActions)
            await commitAction();

        return true;
    }
}