using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;

namespace Elsa.Persistence.Requests;

public record FindWorkflowDefinitionByName(string Name, VersionOptions VersionOptions) : IRequest<WorkflowDefinition?>;