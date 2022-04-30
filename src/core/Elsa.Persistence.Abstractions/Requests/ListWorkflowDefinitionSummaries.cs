using Elsa.Mediator.Services;
using Elsa.Persistence.Models;

namespace Elsa.Persistence.Requests;

public record ListWorkflowDefinitionSummaries(VersionOptions? VersionOptions = default, PageArgs? PageArgs = default) : IRequest<Page<WorkflowDefinitionSummary>>;