using System.Collections.Generic;
using Elsa.Models;
using NodaTime;

namespace Elsa.Server.Api.Endpoints.WorkflowInstances
{
    public record WorkflowInstanceSummaryModel
    {
        public string Id { get; set; } = default!;
        public string DefinitionId { get; set; } = default!;
        public string? TenantId { get; set; }
        public int Version { get; set; }
        public WorkflowStatus WorkflowStatus { get; set; }
        public string? CorrelationId { get; set; }
        public string? ContextType { get; set; }
        public string? ContextId { get; set; }
        public string? Name { get; set; }
        public Instant CreatedAt { get; set; }
        public Instant? LastExecutedAt { get; set; }
        public Instant? FinishedAt { get; set; }
        public Instant? CancelledAt { get; set; }
        public Instant? FaultedAt { get; set; }
    }
    
    public record RetryWorkflowRequest(bool RunImmediately)
    {
        /// <summary>
        /// Set to true to run the revived workflow immediately, set to false to enqueue the revived workflow for execution.
        /// </summary>
        public bool RunImmediately { get; set; } = RunImmediately;
    }
    
    public record BulkRetryWorkflowsRequest(ICollection<string> WorkflowInstanceIds)
    {
    }
    
    public record BulkDeleteWorkflowsRequest(ICollection<string> WorkflowInstanceIds)
    {
    }
    
    public record ExecuteWorkflowInstanceRequestModel(string? ActivityId, WorkflowInput? Input);

    public record ExecuteWorkflowInstanceResponseModel(bool Executed, string? ActivityId, WorkflowInstance? WorkflowInstance);

    public record DispatchWorkflowInstanceRequestModel(string? ActivityId, WorkflowInput? Input);

    public record DispatchWorkflowInstanceResponseModel();
}