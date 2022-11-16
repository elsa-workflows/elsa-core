using Elsa.Mediator.Services;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Notifications;

public record WorkflowBookmarksSaved(List<Bookmark> Bookmarks) : INotification;