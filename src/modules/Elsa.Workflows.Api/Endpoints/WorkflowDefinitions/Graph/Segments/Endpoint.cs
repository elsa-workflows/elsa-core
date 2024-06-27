using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Serialization.Helpers;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Graph.Segments;

[PublicAPI]
internal class Nodes(IWorkflowDefinitionService workflowDefinitionService, IApiSerializer apiSerializer, ActivityWriter activityWriter) : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Get("/workflow-definitions/subgraph/segments/{id}");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(request.Id, cancellationToken);

        if (workflowGraph?.Root == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var nodeId = request.ChildNodeId;
        var childNode = workflowGraph.NodeIdLookup[nodeId];
        var ancestors = childNode.Ancestors().Reverse().ToList();
        var serializerOptions = apiSerializer.GetOptions().Clone().WithConverters(new RootActivityNodeConverter(activityWriter));
        var segments = new List<ActivityPathSegment>();
        var currentNode = ancestors.LastOrDefault();
        var previousNode = childNode;

        while (currentNode != null)
        {
            if (currentNode.Activity.Type is not "Elsa.Workflow" and not "Elsa.Flowchart")
            {
                var currentActivity = currentNode.Activity;
                var currentPort = previousNode?.Port ?? "Root";
                var activityName = currentActivity.Name ?? currentActivity.Type;
                var segment = new ActivityPathSegment(currentNode.NodeId, currentActivity.Id, currentActivity.Type, currentPort, activityName);
                segments.Add(segment);
            }

            previousNode = currentNode;
            currentNode = currentNode.Parents.FirstOrDefault();
        }

        var leafSegment = segments.LastOrDefault();
        var container = leafSegment == null ? ancestors.Last() : ancestors.Last(x => x.Activity.Id == leafSegment.ActivityId);
        var response = new Response(childNode, container, segments);
        await HttpContext.Response.WriteAsJsonAsync(response, serializerOptions, cancellationToken);
    }
}