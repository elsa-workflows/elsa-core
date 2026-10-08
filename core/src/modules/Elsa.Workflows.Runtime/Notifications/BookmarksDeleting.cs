using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// Triggered when bookmarks are being deleted.
/// </summary>
/// <param name="Bookmarks">The bookmarks being deleted.</param>
public record BookmarksDeleting(ICollection<StoredBookmark> Bookmarks) : INotification;