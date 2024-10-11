using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
[Obsolete("Use IWorkflowRuntime, IWorkflowRunner and IWorkflowInvoker services instead.")]
public class WorkflowHostFactory : IWorkflowHostFactory
{
    private readonly IIdentityGenerator _identityGenerator;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowHostFactory"/> class.
    /// </summary>
    public WorkflowHostFactory(IIdentityGenerator identityGenerator, IServiceProvider serviceProvider)
    {
        _identityGenerator = identityGenerator;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<IWorkflowHost> CreateAsync(WorkflowGraph workflowGraph, WorkflowHostOptions? options = null, CancellationToken cancellationToken = default)
    {
        var workflow = workflowGraph.Workflow;
        var newWorkflowInstanceId = options?.NewWorkflowInstanceId ?? _identityGenerator.GenerateId();
        var workflowState = new WorkflowState
        {
            Id = newWorkflowInstanceId,
            DefinitionId = workflow.Identity.DefinitionId,
            DefinitionVersion = workflow.Identity.Version
        };

        return await CreateAsync(workflowGraph, workflowState, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IWorkflowHost> CreateAsync(WorkflowGraph workflowGraph, WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var workflowHost = (IWorkflowHost)ActivatorUtilities.CreateInstance<WorkflowHost>(_serviceProvider, workflowGraph, workflowState);
        return Task.FromResult(workflowHost);
    }
}