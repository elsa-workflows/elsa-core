using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;

namespace Elsa.Runtime.Notifications;

public record WorkflowBookmarksSaved(List<WorkflowBookmark> Bookmarks) : INotification;