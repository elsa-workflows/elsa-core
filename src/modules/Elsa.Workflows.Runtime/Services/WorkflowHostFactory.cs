using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowHostFactory : IWorkflowHostFactory
{
    private readonly IIdentityGenerator _identityGenerator;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowHostFactory"/> class.
    /// </summary>
    public WorkflowHostFactory(IIdentityGenerator identityGenerator, IServiceProvider serviceProvider, IWorkflowDefinitionService workflowDefinitionService)
    {
        _identityGenerator = identityGenerator;
        _serviceProvider = serviceProvider;
        _workflowDefinitionService = workflowDefinitionService;
    }

    /// <inheritdoc />
    public async Task<IWorkflowHost> CreateAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        return await CreateAsync(workflow, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IWorkflowHost> CreateAsync(Workflow workflow, WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var workflowHost = (IWorkflowHost)ActivatorUtilities.CreateInstance<WorkflowHost>(_serviceProvider, workflow, workflowState);
        return Task.FromResult(workflowHost);
    }

    /// <inheritdoc />
    public Task<IWorkflowHost> CreateAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        var workflowState = new WorkflowState
        {
            Id = _identityGenerator.GenerateId(),
            DefinitionId = workflow.Identity.DefinitionId,
            DefinitionVersion = workflow.Identity.Version
        };

        return CreateAsync(workflow, workflowState, cancellationToken);
    }
}