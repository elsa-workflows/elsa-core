using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowHostFactory(IIdentityGenerator identityGenerator, IServiceProvider serviceProvider, IWorkflowDefinitionService workflowDefinitionService)
    : IWorkflowHostFactory
{
    /// <inheritdoc />
    public async Task<IWorkflowHost?> CreateAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var workflow = await workflowDefinitionService.FindWorkflowAsync(definitionId, versionOptions, cancellationToken);

        if (workflow == null)
            return default;

        return await CreateAsync(workflow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IWorkflowHost> CreateAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
    {
        var workflow = await workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        return await CreateAsync(workflow, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IWorkflowHost> CreateAsync(Workflow workflow, WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var workflowHost = (IWorkflowHost)ActivatorUtilities.CreateInstance<WorkflowHost>(serviceProvider, workflow, workflowState);
        return Task.FromResult(workflowHost);
    }

    /// <inheritdoc />
    public Task<IWorkflowHost> CreateAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        var workflowState = new WorkflowState
        {
            Id = identityGenerator.GenerateId(),
            DefinitionId = workflow.Identity.DefinitionId,
            DefinitionVersion = workflow.Identity.Version
        };

        return CreateAsync(workflow, workflowState, cancellationToken);
    }
}