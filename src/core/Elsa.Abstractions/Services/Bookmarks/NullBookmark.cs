namespace Elsa.Services.Bookmarks
{
    public class NullBookmark : IBookmark
    {
        public static readonly IBookmark Instance = new NullBookmark();
    }
}