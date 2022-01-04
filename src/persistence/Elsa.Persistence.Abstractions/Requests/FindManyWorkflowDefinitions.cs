using System.Collections.Generic;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Models;

namespace Elsa.Persistence.Requests;

public record FindManyWorkflowDefinitions(string[] DefinitionIds, VersionOptions? VersionOptions = default) : IRequest<IEnumerable<WorkflowSummary>>;