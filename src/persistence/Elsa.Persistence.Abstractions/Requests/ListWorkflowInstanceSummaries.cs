using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;

namespace Elsa.Persistence.Requests;

public record ListWorkflowInstanceSummaries(
    string? SearchTerm = default,
    string? DefinitionId = default,
    int? Version = default,
    string? CorrelationId = default,
    WorkflowStatus? WorkflowStatus = default,
    OrderBy OrderBy = OrderBy.Created,
    OrderDirection OrderDirection = OrderDirection.Ascending,
    int Skip = 0,
    int Take = 50
) : IRequest<PagedList<WorkflowInstanceSummary>>;