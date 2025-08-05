using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management;
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
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        var nodeId = request.ChildNodeId;

        if (!workflowGraph.NodeIdLookup.TryGetValue(nodeId, out var childNode))
        {
            AddError("Unknown node ID");
            await Send.ErrorsAsync(cancellation: cancellationToken);
            return;
        }
        
        var ancestors = childNode.Ancestors().ToList();
        var serializerOptions = apiSerializer.GetOptions().Clone().WithConverters(new RootActivityNodeConverter(activityWriter));
        var segments = new Stack<ActivityPathSegment>();
        var currentNode = ancestors.FirstOrDefault();
        var previousNode = childNode;

        while (currentNode != null)
        {
            if (currentNode.Activity is not Workflow and not Flowchart)
            {
                var currentActivity = currentNode.Activity;
                var currentPort = previousNode.Port;
                var activityName = currentActivity.Name ?? currentActivity.Type;
                var segment = new ActivityPathSegment(currentNode.NodeId, currentActivity.Id, currentActivity.Type, currentPort, activityName);
                segments.Push(segment);
            }

            previousNode = currentNode;
            currentNode = currentNode.Parents.FirstOrDefault();
        }

        var leafSegment = segments.LastOrDefault();
        var container = leafSegment == null ? ancestors.LastOrDefault() ?? childNode : ancestors.Last(x => x.Activity.Id == leafSegment.ActivityId);
        var response = new Response(childNode, container, segments.ToList());
        await HttpContext.Response.WriteAsJsonAsync(response, serializerOptions, cancellationToken);
    }
}