using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Requests;

public record FindLatestAndPublishedWorkflows(string DefinitionId) : IRequest<IEnumerable<WorkflowDefinition>>;