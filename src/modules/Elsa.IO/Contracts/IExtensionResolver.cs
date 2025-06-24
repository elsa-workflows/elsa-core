namespace Elsa.IO.Contracts;

/// <summary>
/// Provides methods to determine appropriate file extensions for various content types.
/// </summary>
public interface IExtensionResolver
{
    /// <summary>
    /// Ensures that a filename has an appropriate file extension based on its content type.
    /// </summary>
    /// <param name="filename">The filename to check and possibly append an extension to.</param>
    /// <param name="content">The content object to examine for determining the appropriate extension.</param>
    /// <returns>The filename with an appropriate extension.</returns>
    string EnsureFileExtension(string filename, object content);
}
