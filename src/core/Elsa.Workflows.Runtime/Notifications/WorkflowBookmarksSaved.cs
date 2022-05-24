using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;

namespace Elsa.Workflows.Runtime.Notifications;

public record WorkflowBookmarksSaved(List<WorkflowBookmark> Bookmarks) : INotification;