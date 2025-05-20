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

    /// <summary>
    /// The local cache directory. Defaults to the system's temp directory.
    /// </summary>
    public string LocalCacheDirectory { get; set; } = Path.GetTempPath();
}