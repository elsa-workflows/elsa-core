using Elsa.Mediator.Services;

namespace Elsa.Workflows.Runtime.Models;

/// <summary>
/// Published when the specified workflow's triggers have been indexed.
/// </summary>
/// <param name="IndexedWorkflowTriggers">Contains the workflow that was indexed and the resulting triggers.</param>
public record WorkflowTriggersIndexed(IndexedWorkflowTriggers IndexedWorkflowTriggers) : INotification;