using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;

namespace Elsa.Runtime.Notifications;

public record WorkflowBookmarksSaved(List<WorkflowBookmark> Bookmarks) : INotification;