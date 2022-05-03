using System;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Api.Endpoints.WorkflowDefinitions;

public class WorkflowsResource : ResourceBase<WorkflowsResource>
{
    public WorkflowsResource(IEndpointRouteBuilder endpoints) : base(endpoints)
    {
    }

    public WorkflowsResource Post() => MapPost("api/workflow-definitions", WorkflowDefinitions.PostAsync);
    public WorkflowsResource List() => MapGet("api/workflow-definitions", WorkflowDefinitions.ListAsync);
    public WorkflowsResource GetMany() => MapGet("api/workflow-definitions/set", WorkflowDefinitions.GetManyAsync);
    public WorkflowsResource Get() => MapGet("api/workflow-definitions/{definitionId}", WorkflowDefinitions.Get);
    public WorkflowsResource Execute() => MapPost("api/workflow-definitions/{definitionId}/execute", WorkflowDefinitions.ExecuteAsync);
    public WorkflowsResource Dispatch() => MapPost("api/workflow-definitions/{definitionId}/dispatch", WorkflowDefinitions.DispatchAsync);
    public WorkflowsResource Delete() => MapDelete("api/workflow-definitions/{definitionId}", WorkflowDefinitions.DeleteAsync);
    public WorkflowsResource Retract() => MapPost("api/workflow-definitions/{definitionId}/retract", WorkflowDefinitions.RetractAsync);
    public WorkflowsResource Publish() => MapPost("api/workflow-definitions/{definitionId}/publish", WorkflowDefinitions.PublishAsync);
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