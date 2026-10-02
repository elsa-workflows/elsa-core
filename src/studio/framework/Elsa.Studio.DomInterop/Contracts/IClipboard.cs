namespace Elsa.Studio.DomInterop.Contracts;

/// <summary>
/// Provides access to the browser's clipboard functionality.
/// </summary>
public interface IClipboard
{
    /// <summary>
    /// Copies the specified text to the clipboard.
    /// </summary>
    /// <param name="text">The text to copy to the clipboard.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task CopyText(string text, CancellationToken cancellationToken = default);
}