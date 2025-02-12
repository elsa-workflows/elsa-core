using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Requests;

/// <summary>
/// A request to find a workflow definition.
/// </summary>
/// <param name="DefinitionId">The ID of the workflow definition.</param>
/// <param name="VersionOptions">The version options.</param>
public record FindWorkflowDefinitionRequest(WorkflowDefinitionHandle Handle) : IRequest<WorkflowDefinition?>;