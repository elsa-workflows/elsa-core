using Elsa.Mediator.Contracts;
using Elsa.Runtime.Models;

namespace Elsa.Runtime.Middleware;

public record WorkflowBookmarksIndexed(IndexedWorkflowBookmarks IndexedWorkflowBookmarks) : INotification;