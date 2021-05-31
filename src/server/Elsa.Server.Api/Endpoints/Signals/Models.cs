using System.Collections.Generic;
using Elsa.Services.Models;

namespace Elsa.Server.Api.Endpoints.Signals
{
    public record DispatchSignalRequest(string? WorkflowInstanceId, string? CorrelationId, object? Input);

    public record DispatchSignalResponse(ICollection<PendingWorkflow> StartedWorkflows);

    public record ExecuteSignalRequest(string? WorkflowInstanceId, string? CorrelationId, object? Input);

    public record ExecuteSignalResponse(ICollection<StartedWorkflow> StartedWorkflows);
}