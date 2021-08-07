namespace Elsa.Services
{
    public interface IBookmarkHasher
    {
        string Hash(IBookmark bookmark);
    }
}