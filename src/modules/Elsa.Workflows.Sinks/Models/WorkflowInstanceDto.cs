using System;
using Elsa.Common.Entities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Sinks.Models;

public class WorkflowInstanceDto
{
    public string Id { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastExecutedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? FaultedAt { get; set; }
    public Workflow Workflow { get; set; } = default!;
    public WorkflowState WorkflowState { get; set; } = default!;
}