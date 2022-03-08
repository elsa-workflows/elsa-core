using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Requests;

public record FindWorkflowBookmark(string BookmarkId) : IRequest<WorkflowBookmark?>;