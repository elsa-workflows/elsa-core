namespace Elsa.Services
{
    public interface IBookmark
    {
        /// <summary>
        /// Compares this bookmark instance with another to check, if the values are equal for the function of the bookmark.
        /// </summary>
        /// <returns><see langword="null"/> if default and no specific compare is done, false if not equal and true otherwise.</returns>
        bool? Compare(IBookmark bookmark) => null;
    }
}