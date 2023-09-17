namespace Elsa.Http.Options;

/// <summary>
/// Provides options for the HTTP file cache.
/// </summary>
public class HttpFileCacheOptions
{
    /// <summary>
    /// The time to live for cached files.
    /// </summary>
    public TimeSpan TimeToLive { get; set; } = TimeSpan.FromDays(7);
}