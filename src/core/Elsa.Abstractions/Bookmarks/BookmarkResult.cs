namespace Elsa.Bookmarks
{
    public record BookmarkResult(IBookmark Bookmark, string? ActivityTypeName = default);
}