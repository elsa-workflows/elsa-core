using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Abstractions;
using Elsa.Alterations.Core.Contexts;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.AlterationHandlers;

/// <summary>
/// Upgrades the version of the workflow instance.
/// </summary>
[UsedImplicitly]
public class MigrateHandler : AlterationHandlerBase<Migrate>
{
    /// <inheritdoc />
    protected override async ValueTask HandleAsync(AlterationContext context, Migrate alteration)
    {
        var workflowDefinitionService = context.ServiceProvider.GetRequiredService<IWorkflowDefinitionService>();
        var definitionId = context.Workflow.Identity.DefinitionId;
        var targetVersion = alteration.TargetVersion;
        var cancellationToken = context.CancellationToken;
        var targetWorkflowDefinition = await workflowDefinitionService.FindWorkflowDefinitionAsync(definitionId, VersionOptions.SpecificVersion(targetVersion), cancellationToken);
        
        if (targetWorkflowDefinition == null)
        {
            context.Fail($"Workflow definition with ID {definitionId} and version {targetVersion} not found");
            return;
        }
        
        var targetWorkflow = await workflowDefinitionService.MaterializeWorkflowAsync(targetWorkflowDefinition, cancellationToken);
        await context.WorkflowExecutionContext.SetWorkflowGraphAsync(targetWorkflow);
        
        context.Succeed();
    }
}