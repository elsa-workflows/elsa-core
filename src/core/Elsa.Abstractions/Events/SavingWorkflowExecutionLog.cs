using System.Collections.Generic;
using Elsa.Models;
using Elsa.Services;
using MediatR;

namespace Elsa.Events;

/// <summary>
/// A domain event that is published when the <see cref="WorkflowExecutionLog"/> about to persist its collection of workflow execution log records.  
/// </summary>
public record SavingWorkflowExecutionLog(IReadOnlyCollection<WorkflowExecutionLogRecord> Records) : INotification;