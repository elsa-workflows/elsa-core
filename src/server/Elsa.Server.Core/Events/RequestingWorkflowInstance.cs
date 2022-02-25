using Elsa.Models;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Server.Core.Events;

/// <summary>
/// A domain event that is published whenever a workflow instance is requested from an API endpoint. 
/// </summary>
public record RequestingWorkflowInstance(WorkflowInstance WorkflowInstance, IWorkflowBlueprint WorkflowBlueprint) : INotification;