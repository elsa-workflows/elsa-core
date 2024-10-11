using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Serialization.Helpers;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Graph;

[PublicAPI]
internal class Graph(IWorkflowDefinitionService workflowDefinitionService, IApiSerializer apiSerializer, ActivityWriter activityWriter) : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Get("/workflow-definitions/subgraph/{id}");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(request.Id, cancellationToken);

        if (workflowGraph == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var parentNode = workflowGraph.NodeIdLookup.TryGetValue(request.ParentNodeId, out var node) ? node : workflowGraph.Root;
        var serializerOptions = apiSerializer.GetOptions().Clone();
        serializerOptions.Converters.Add(new RootActivityNodeConverter(activityWriter));
        await HttpContext.Response.WriteAsJsonAsync(parentNode, serializerOptions, cancellationToken);
    }
}