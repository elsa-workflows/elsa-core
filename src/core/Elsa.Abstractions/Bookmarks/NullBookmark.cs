namespace Elsa.Bookmarks
{
    public class NullBookmark : IBookmark
    {
        public static readonly IBookmark Instance = new NullBookmark();
    }
}