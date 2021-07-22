namespace Elsa.Services
{
    public record BookmarkResult(IBookmark Bookmark, string? ActivityTypeName = default);
}