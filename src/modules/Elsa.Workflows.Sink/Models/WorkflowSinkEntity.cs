using System;
using Elsa.Common.Entities;

namespace Elsa.Workflows.Sink.Models;

public class WorkflowSink : Entity
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastExecutedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? FaultedAt { get; set; }
}