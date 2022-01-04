using Elsa.Mediator.Contracts;
using Elsa.Persistence.Models;

namespace Elsa.Persistence.Requests;

public record ListWorkflowSummaries(VersionOptions? VersionOptions = default, int? Skip = default, int? Take = default) : IRequest<PagedList<WorkflowSummary>>;