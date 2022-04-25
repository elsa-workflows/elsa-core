using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;

namespace Elsa.Runtime.Notifications;

public record WorkflowBookmarksDeleted(List<WorkflowBookmark> Bookmarks) : INotification;