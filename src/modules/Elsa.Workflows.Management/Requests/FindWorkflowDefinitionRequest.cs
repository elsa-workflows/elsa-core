using Elsa.Common.Models;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Requests;

/// <summary>
/// A request to find a workflow definition.
/// </summary>
/// <param name="DefinitionId">The ID of the workflow definition.</param>
/// <param name="VersionOptions">The version options.</param>
public record FindWorkflowDefinitionRequest(string DefinitionId, VersionOptions VersionOptions) : IRequest<WorkflowDefinition?>;