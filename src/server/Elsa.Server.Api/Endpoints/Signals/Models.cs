using System.Collections.Generic;
using Elsa.Services.Models;

namespace Elsa.Server.Api.Endpoints.Signals
{
    public record DispatchSignalRequest
    {
        // Need this for Swagger.
        public DispatchSignalRequest()
        {
        }
        
        public DispatchSignalRequest(string? workflowInstanceId, string? correlationId, object? input)
        {
            WorkflowInstanceId = workflowInstanceId;
            CorrelationId = correlationId;
            Input = input;
        }

        public string? WorkflowInstanceId { get; init; }
        public string? CorrelationId { get; init; }
        public object? Input { get; init; }
    }

    public record DispatchSignalResponse
    {
        // Need this for Swagger.
        public DispatchSignalResponse()
        {
        }
        
        public DispatchSignalResponse(ICollection<PendingWorkflow> startedWorkflows)
        {
            StartedWorkflows = startedWorkflows;
        }

        public ICollection<PendingWorkflow> StartedWorkflows { get; init; } = default!;
    }

    public record ExecuteSignalRequest
    {
        // Need this for Swagger.
        public ExecuteSignalRequest()
        {
        }
        
        public ExecuteSignalRequest(string? workflowInstanceId, string? correlationId, object? input)
        {
            WorkflowInstanceId = workflowInstanceId;
            CorrelationId = correlationId;
            Input = input;
        }
        
        public string? WorkflowInstanceId { get; init; }
        public string? CorrelationId { get; init; }
        public object? Input { get; init; }
    }

    public record ExecuteSignalResponse
    {
        // Need this for Swagger.
        public ExecuteSignalResponse()
        {
        }

        public ExecuteSignalResponse(ICollection<StartedWorkflow> startedWorkflows)
        {
            StartedWorkflows = startedWorkflows;
        }
        
        public ICollection<StartedWorkflow> StartedWorkflows { get; init; } = new List<StartedWorkflow>();
    }
}