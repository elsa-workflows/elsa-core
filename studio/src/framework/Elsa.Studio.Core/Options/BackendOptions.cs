namespace Elsa.Studio.Options;

/// <summary>
/// Provides options about the backend.
/// </summary>
public class BackendOptions
{
    /// <summary>
    /// The URL of the backend.
    /// </summary>
    public Uri Url { get; set; } = default!;
}