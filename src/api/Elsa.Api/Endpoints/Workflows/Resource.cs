using System;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Api.Endpoints.Workflows;

public class WorkflowsResource : ResourceBase<WorkflowsResource>
{
    public WorkflowsResource(IEndpointRouteBuilder endpoints) : base(endpoints)
    {
    }

    public WorkflowsResource Post() => MapPost("api/workflows", Workflows.PostAsync);
    public WorkflowsResource List() => MapGet("api/workflows", Workflows.ListAsync);
    public WorkflowsResource GetMany() => MapGet("api/workflows/set", Workflows.GetManyAsync);
    public WorkflowsResource Get() => MapGet("api/workflows/{definitionId}", Workflows.Get);
    public WorkflowsResource Execute() => MapPost("api/workflows/{definitionId}/execute", Workflows.ExecuteAsync);
    public WorkflowsResource Dispatch() => MapPost("api/workflows/{definitionId}/dispatch", Workflows.DispatchAsync);
    public WorkflowsResource Delete() => MapDelete("api/workflows/{definitionId}", Workflows.DeleteAsync);
    public WorkflowsResource Retract() => MapPost("api/workflows/{definitionId}/retract", Workflows.RetractAsync);
    public WorkflowsResource Publish() => MapPost("api/workflows/{definitionId}/publish", Workflows.PublishAsync);
}

public static class WorkflowsResourceExtensions
{
    public static IEndpointRouteBuilder MapWorkflows(this IEndpointRouteBuilder endpoints, Action<WorkflowsResource> configureResource)
    {
        configureResource(new(endpoints));
        return endpoints;
    }

    public static IEndpointRouteBuilder MapWorkflows(this IEndpointRouteBuilder endpoints) => endpoints.MapWorkflows(x => x
        .Post()
        .Execute()
        .Dispatch()
        .List()
        .GetMany()
        .Get()
        .Delete()
        .Retract()
        .Publish()
    );
}