namespace Elsa.Caching.Options;

/// <summary>
/// Provides options for configuring caching.
/// </summary>
public class CachingOptions
{
    /// <summary>
    /// Gets or sets the duration for which cache entries are stored.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(1);
}