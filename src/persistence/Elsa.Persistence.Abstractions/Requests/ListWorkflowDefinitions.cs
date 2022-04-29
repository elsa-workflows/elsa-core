using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;

namespace Elsa.Persistence.Requests;

public record ListWorkflowDefinitions(VersionOptions? VersionOptions = default, PageArgs? PageArgs = default) : IRequest<Page<WorkflowDefinition>>;