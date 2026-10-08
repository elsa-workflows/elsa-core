using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// Triggered when bookmarks have been deleted.
/// </summary>
/// <param name="Bookmarks">The bookmarks that have been deleted.</param>
public record BookmarksDeleted(ICollection<StoredBookmark> Bookmarks) : INotification;