using Elsa.Extensions;
using Elsa.Http;
using Elsa.Http.Bookmarks;
using Elsa.Http.OpenApi.Contracts;
using Elsa.Http.OpenApi.Models;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Common.Models;
using Elsa.Workflows;
using System.Net;
using Elsa.Scheduling.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;

namespace Elsa.Http.OpenApi.Services;

/// <summary>
/// Service for extracting HTTP endpoint definitions from Elsa workflows.
/// </summary>
public class WorkflowEndpointExtractor : IWorkflowEndpointExtractor
{
    private readonly ITriggerStore _triggerStore;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;

    public WorkflowEndpointExtractor(ITriggerStore triggerStore, IWorkflowDefinitionStore workflowDefinitionStore)
    {
        _triggerStore = triggerStore;
        _workflowDefinitionStore = workflowDefinitionStore;
    }

    public async Task<List<EndpointDefinition>> ExtractEndpointsAsync(CancellationToken cancellationToken = default)
    {
        var endpoints = new List<EndpointDefinition>();

        var httpEndpointTypeName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        var triggerFilter = new TriggerFilter
        {
            Name = httpEndpointTypeName
        };
        var triggers = (await _triggerStore.FindManyAsync(triggerFilter, cancellationToken)).ToList();

        var filteredTriggers = triggers.Where(x => x.Name == httpEndpointTypeName && x.Payload != null);

        // Group triggers by workflow definition ID to minimize database queries
        var triggersByWorkflow = filteredTriggers.GroupBy(t => t.WorkflowDefinitionId);

        foreach (var workflowGroup in triggersByWorkflow)
        {
            var workflowDefinitionId = workflowGroup.Key;
            
            // Get workflow definition information
            var filter = new WorkflowDefinitionFilter { DefinitionId = workflowDefinitionId };
            var workflowDefinition = await _workflowDefinitionStore.FindAsync(filter, cancellationToken);
            
            foreach (var trigger in workflowGroup)
            {
                var payload = trigger.GetPayload<HttpEndpointBookmarkPayload>();

                endpoints.Add(new EndpointDefinition(
                    Path: payload.Path,
                    Method: payload.Method,
                    Summary: null, // Could be enhanced to extract from activity properties
                    WorkflowDefinitionId: workflowDefinitionId,
                    WorkflowDefinitionName: workflowDefinition?.Name ?? "Unknown Workflow",
                    WorkflowVersion: workflowDefinition?.Version
                ));
            }
        }

        return endpoints;
    }
}
