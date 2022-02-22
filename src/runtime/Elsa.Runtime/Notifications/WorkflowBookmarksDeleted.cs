using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;

namespace Elsa.Runtime.Notifications;

public record WorkflowBookmarksDeleted(List<WorkflowBookmark> Bookmarks) : INotification;