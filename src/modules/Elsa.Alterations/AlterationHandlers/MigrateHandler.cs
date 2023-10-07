using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Abstractions;
using Elsa.Alterations.Core.Contexts;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.AlterationHandlers;

/// <summary>
/// Upgrades the version of the workflow instance.
/// </summary>
public class MigrateHandler : AlterationHandlerBase<Migrate>
{
    /// <inheritdoc />
    protected override async ValueTask HandleAsync(AlterationContext context, Migrate alteration)
    {
        var workflowDefinitionService = context.ServiceProvider.GetRequiredService<IWorkflowDefinitionService>();
        var definitionId = context.Workflow.Identity.DefinitionId;
        var targetVersion = alteration.TargetVersion;
        var cancellationToken = context.CancellationToken;
        var targetWorkflowDefinition = await workflowDefinitionService.FindAsync(definitionId, VersionOptions.SpecificVersion(targetVersion), cancellationToken);
        
        if (targetWorkflowDefinition == null)
        {
            context.Fail($"Workflow definition with ID {definitionId} and version {targetVersion} not found");
            return;
        }
        
        var targetWorkflow = await workflowDefinitionService.MaterializeWorkflowAsync(targetWorkflowDefinition, cancellationToken);
        await context.WorkflowExecutionContext.SetWorkflowAsync(targetWorkflow);
        
        context.Succeed();
    }
}