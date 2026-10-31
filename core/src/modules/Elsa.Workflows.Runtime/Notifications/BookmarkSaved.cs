using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// Triggered when a bookmark has been saved.
/// </summary>
/// <param name="Bookmark">The bookmark being saved.</param>
public record BookmarkSaved(StoredBookmark Bookmark) : INotification;