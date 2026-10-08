namespace Elsa.Studio.DomInterop.Contracts;

/// <summary>
/// Provides JS interop methods for the files module.
/// </summary>
public interface IFiles
{
    /// <summary>
    /// Downloads a file from a stream.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="stream">The stream to download from.</param>
    Task DownloadFileFromStreamAsync(string fileName, Stream stream);
}