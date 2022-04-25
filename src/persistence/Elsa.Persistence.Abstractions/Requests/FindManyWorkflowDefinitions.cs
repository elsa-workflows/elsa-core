using Elsa.Mediator.Services;
using Elsa.Persistence.Models;

namespace Elsa.Persistence.Requests;

public record FindManyWorkflowDefinitions(string[] DefinitionIds, VersionOptions? VersionOptions = default) : IRequest<IEnumerable<WorkflowSummary>>;