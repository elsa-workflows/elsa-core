using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Requests;

public record FindWorkflowBookmark(string BookmarkId) : IRequest<WorkflowBookmark?>;