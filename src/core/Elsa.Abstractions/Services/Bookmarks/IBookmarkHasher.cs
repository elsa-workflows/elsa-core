namespace Elsa.Services.Bookmarks
{
    public interface IBookmarkHasher
    {
        string Hash(IBookmark bookmark);
    }
}