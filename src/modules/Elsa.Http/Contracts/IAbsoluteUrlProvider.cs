namespace Elsa.Http.Contracts;

/// <summary>
/// Provides a way to convert a relative URL to an absolute URL.
/// </summary>
public interface IAbsoluteUrlProvider
{
    /// <summary>
    /// Converts a relative URL to an absolute URL.
    /// </summary>
    /// <param name="relativePath">The relative URL.</param>
    /// <returns>The absolute URL.</returns>
    Uri ToAbsoluteUrl(string relativePath);
}