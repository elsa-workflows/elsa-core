using Elsa.Labels.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Labels.Extensions;

public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps default routes for Elsa API endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapLabelApiEndpoints(this IEndpointRouteBuilder endpoints, string basePattern = "elsa/api")
    {
        void Map(string routeName, string pattern, object defaults) => endpoints.MapAreaControllerRoute($"Elsa.{routeName}", AreaNames.Elsa, $"{basePattern}/{pattern}", defaults);
        
        // Labels
        Map("Labels.Create", "labels", new { Controller = ControllerNames.Labels, Action = "Create" });
        Map("Labels.Update", "labels/{id}", new { Controller = ControllerNames.Labels, Action = "Update" });
        Map("Labels.Get", "labels/{id}", new { Controller = ControllerNames.Labels, Action = "Get" });
        Map("Labels.Delete", "labels/{id}", new { Controller = ControllerNames.Labels, Action = "Delete" });
        Map("Labels.List", "labels", new { Controller = ControllerNames.Labels, Action = "List" });

        // Workflow Definition Labels
        Map("WorkflowDefinitions.Labels.Update", "workflow-definitions/{id}/labels", new { Controller = ControllerNames.WorkflowDefinitionLabels, Action = "Update" });
        Map("WorkflowDefinitions.Labels.Get", "workflow-definitions/{id}/labels", new { Controller = ControllerNames.WorkflowDefinitionLabels, Action = "Get" });

        return endpoints;
    }
}