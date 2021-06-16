namespace Elsa.Services.Bookmarks
{
    public record BookmarkResult(IBookmark Bookmark, string? ActivityTypeName = default);
}