namespace Elsa.Bookmarks
{
    public interface IBookmarkHasher
    {
        string Hash(IBookmark bookmark);
    }
}