using System;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Api.Endpoints.WorkflowInstances;

public class WorkflowInstancesResource : ResourceBase<WorkflowInstancesResource>
{
    public WorkflowInstancesResource(IEndpointRouteBuilder endpoints) : base(endpoints)
    {
    }

    public WorkflowInstancesResource List() => MapGet("api/workflow-instances", WorkflowInstances.ListAsync);
    public WorkflowInstancesResource Get() => MapGet("api/workflow-instances/{id}", WorkflowInstances.GetAsync);
}

public static class WorkflowsResourceExtensions
{
    public static IEndpointRouteBuilder MapWorkflowInstances(this IEndpointRouteBuilder endpoints, Action<WorkflowInstancesResource> configureResource)
    {
        configureResource(new(endpoints));
        return endpoints;
    }

    public static IEndpointRouteBuilder MapWorkflowInstances(this IEndpointRouteBuilder endpoints) => endpoints.MapWorkflowInstances(x => x
        .List()
        .Get()
    );
}