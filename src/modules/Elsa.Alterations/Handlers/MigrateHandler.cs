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

namespace Elsa.Alterations.Handlers;

/// <summary>
/// Upgrades the version of the workflow instance.
/// </summary>
public class MigrateHandler : AlterationHandlerBase<Migrate>
{
    /// <inheritdoc />
    protected override async ValueTask HandleAsync(AlterationHandlerContext context, Migrate alteration)
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
        await UpgradeAsync(context.WorkflowExecutionContext, targetWorkflow, cancellationToken);
    }

    private async Task UpgradeAsync(WorkflowExecutionContext workflowExecutionContext, Workflow workflow, CancellationToken cancellationToken = default)
    {
        var useActivityIdAsNodeId = workflow.CreatedWithModernTooling();
        var activityVisitor = workflowExecutionContext.GetRequiredService<IActivityVisitor>();
        var graph = await activityVisitor.VisitAsync(workflow, useActivityIdAsNodeId, cancellationToken);
        var flattenedList = graph.Flatten().ToList();
        var activityTypes = flattenedList.Select(x => x.Activity.GetType()).Distinct().ToList();
        var activityRegistry = workflowExecutionContext.GetRequiredService<IActivityRegistry>();
        await activityRegistry.RegisterAsync(activityTypes, cancellationToken);
        
        var needsIdentityAssignment = flattenedList.Any(x => string.IsNullOrEmpty(x.Activity.Id));

        if (needsIdentityAssignment)
        {
            var identityGraphService = workflowExecutionContext.GetRequiredService<IIdentityGraphService>();
            identityGraphService.AssignIdentities(flattenedList);
        }
        
        workflowExecutionContext.Workflow = workflow;
        workflowExecutionContext.Graph = graph;
    }
}