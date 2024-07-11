using Elsa.Mediator.Contracts;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Notifications;

public record WorkflowBookmarksDeleted(List<Bookmark> Bookmarks) : INotification;