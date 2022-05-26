using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Workflows.Api.Extensions;

public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps default routes for Elsa API endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapWorkflowManagementApiEndpoints(this IEndpointRouteBuilder endpoints, string basePattern = "elsa/api")
    {
        void Map(string routeName, string pattern, object defaults) => endpoints.MapAreaControllerRoute($"Elsa.{routeName}", AreaNames.Elsa, $"{basePattern}/{pattern}", defaults);

        // Activity Descriptors.
        Map("ActivityDescriptors.List", "descriptors/activities", new { Controller = ControllerNames.ActivityDescriptors, Action = "List" });

        // Events.
        Map("Events.Trigger", "events/{eventName}/trigger", new { Controller = ControllerNames.Events, Action = "Trigger" });

        // Workflow Definitions.
        Map("WorkflowDefinitions.ImportNew", "workflow-definitions/import", new { Controller = ControllerNames.WorkflowDefinitions, Action = "Import" });
        Map("WorkflowDefinitions.ImportExisting", "workflow-definitions/{definitionId}/import", new { Controller = ControllerNames.WorkflowDefinitions, Action = "Import" });
        Map("WorkflowDefinitions.Post", "workflow-definitions", new { Controller = ControllerNames.WorkflowDefinitions, Action = "Post" });
        Map("WorkflowDefinitions.List", "workflow-definitions", new { Controller = ControllerNames.WorkflowDefinitions, Action = "List" });
        Map("WorkflowDefinitions.Get", "workflow-definitions/{definitionId}", new { Controller = ControllerNames.WorkflowDefinitions, Action = "Get" });
        Map("WorkflowDefinitions.Delete", "workflow-definitions/{definitionId}", new { Controller = ControllerNames.WorkflowDefinitions, Action = "Delete" });
        Map("WorkflowDefinitions.Publish", "workflow-definitions/{definitionId}/publish", new { Controller = ControllerNames.WorkflowDefinitions, Action = "Publish" });
        Map("WorkflowDefinitions.Retract", "workflow-definitions/{definitionId}/retract", new { Controller = ControllerNames.WorkflowDefinitions, Action = "Retract" });
        Map("WorkflowDefinitions.Dispatch", "workflow-definitions/{definitionId}/dispatch", new { Controller = ControllerNames.WorkflowDefinitions, Action = "Dispatch" });
        Map("WorkflowDefinitions.Execute", "workflow-definitions/{definitionId}/execute", new { Controller = ControllerNames.WorkflowDefinitions, Action = "Execute" });
        Map("WorkflowDefinitions.Export", "workflow-definitions/{definitionId}/export", new { Controller = ControllerNames.WorkflowDefinitions, Action = "Export" });
        Map("WorkflowDefinitions.BulkDelete", "bulk-actions/delete/workflow-definitions/by-definition-id", new { Controller = ControllerNames.WorkflowDefinitions, Action = "BulkDelete" });
        Map("WorkflowDefinitions.BulkPublish", "bulk-actions/publish/workflow-definitions/by-definition-id", new { Controller = ControllerNames.WorkflowDefinitions, Action = "BulkPublish" });
        Map("WorkflowDefinitions.BulkRetract", "bulk-actions/retract/workflow-definitions/by-definition-id", new { Controller = ControllerNames.WorkflowDefinitions, Action = "BulkRetract" });

        // Workflow Instances.
        Map("WorkflowInstances.BulkDelete", "bulk-actions/delete/workflow-instances/by-id", new { Controller = ControllerNames.WorkflowInstances, Action = "BulkDelete" });
        Map("WorkflowInstances.BulkCancel", "bulk-actions/cancel/workflow-instances/by-id", new { Controller = ControllerNames.WorkflowInstances, Action = "BulkCancel" });
        Map("WorkflowInstances.Get", "workflow-instances/{id}", new { Controller = ControllerNames.WorkflowInstances, Action = "Get" });
        Map("WorkflowInstances.Delete", "workflow-instances/{id}", new { Controller = ControllerNames.WorkflowInstances, Action = "Delete" });
        Map("WorkflowInstances.List", "workflow-instances", new { Controller = ControllerNames.WorkflowInstances, Action = "List" });

        return endpoints;
    }
}