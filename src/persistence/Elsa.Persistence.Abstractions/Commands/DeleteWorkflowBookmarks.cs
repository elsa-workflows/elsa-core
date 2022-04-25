using Elsa.Mediator.Services;

namespace Elsa.Persistence.Commands;

/// <summary>
/// Represents a command to delete all of the specified <see cref="BookmarkIds"/>.
/// </summary>
public record DeleteWorkflowBookmarks : ICommand<int>
{
    public DeleteWorkflowBookmarks(IEnumerable<string> bookmarkIds) => BookmarkIds = bookmarkIds.ToList();
    public ICollection<string> BookmarkIds { get; set; }
}