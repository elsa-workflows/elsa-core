using Elsa.Common.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowHostFactory : IWorkflowHostFactory
{
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowHostFactory"/> class.
    /// </summary>
    public WorkflowHostFactory(IWorkflowDefinitionService workflowDefinitionService, IIdentityGenerator identityGenerator, IServiceProvider serviceProvider)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _identityGenerator = identityGenerator;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<IWorkflowHost?> CreateAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var workflowDefinition = await _workflowDefinitionService.FindAsync(definitionId, versionOptions, cancellationToken);
        
        if(workflowDefinition == null)
            return default;
        
        return await CreateAsync(workflowDefinition, cancellationToken);
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