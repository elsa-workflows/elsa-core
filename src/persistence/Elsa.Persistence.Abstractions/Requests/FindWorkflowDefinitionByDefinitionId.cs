using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;

namespace Elsa.Persistence.Requests;

public record FindWorkflowDefinitionByDefinitionId(string DefinitionId, VersionOptions VersionOptions) : IRequest<WorkflowDefinition?>;