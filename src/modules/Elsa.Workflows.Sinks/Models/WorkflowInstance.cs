using System;
using Elsa.Common.Entities;

namespace Elsa.Workflows.Sinks.Models;

public class WorkflowInstance : Entity
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastExecutedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? FaultedAt { get; set; }
}