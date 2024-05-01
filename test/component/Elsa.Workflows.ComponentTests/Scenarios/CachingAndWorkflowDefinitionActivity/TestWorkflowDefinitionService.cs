using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.ComponentTests.Scenarios.CachingAndWorkflowDefinitionActivity;

public class TestWorkflowDefinitionService(IWorkflowDefinitionService decoratedService) : IWorkflowDefinitionService
{
    public Task<WorkflowGraph> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        return decoratedService.MaterializeWorkflowAsync(definition, cancellationToken);
    }

    public Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        return decoratedService.FindWorkflowDefinitionAsync(definitionId, versionOptions, cancellationToken);
    }

    public Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        return decoratedService.FindWorkflowDefinitionAsync(definitionVersionId, cancellationToken);
    }

    public Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return decoratedService.FindWorkflowDefinitionAsync(filter, cancellationToken);
    }

    public Task<WorkflowGraph?> FindWorkflowGraphAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        return decoratedService.FindWorkflowGraphAsync(definitionId, versionOptions, cancellationToken);
    }

    public Task<WorkflowGraph?> FindWorkflowGraphAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        return decoratedService.FindWorkflowGraphAsync(definitionVersionId, cancellationToken);
    }

    public Task<WorkflowGraph?> FindWorkflowGraphAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return decoratedService.FindWorkflowGraphAsync(filter, cancellationToken);
    }
}