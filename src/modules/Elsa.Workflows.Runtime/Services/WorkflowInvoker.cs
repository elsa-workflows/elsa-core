using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowInvoker(IWorkflowHostFactory workflowHostFactory, IWorkflowGraphBuilder workflowGraphBuilder) : IWorkflowInvoker
{
    /// <inheritdoc />
    public async Task<RunWorkflowResult> InvokeAsync(Workflow workflow, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var workflowGraph = await workflowGraphBuilder.BuildAsync(workflow, cancellationToken);
        return await InvokeAsync(workflowGraph, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult> InvokeAsync(WorkflowGraph workflowGraph, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var workflowHostOptions = new WorkflowHostOptions
        {
            NewWorkflowInstanceId = options?.WorkflowInstanceId
        };
        var workflowHost = await workflowHostFactory.CreateAsync(workflowGraph, workflowHostOptions, cancellationToken);

        if (!await workflowHost.CanStartWorkflowAsync(options, cancellationToken))
            throw new Exception("Workflow cannot be started.");

        return await workflowHost.RunWorkflowAsync(options, cancellationToken);
    }
}