namespace Elsa.Retention.Options;

/// <summary>
///     Retention options
/// </summary>
public class CleanupOptions
{
    /// <summary>
    ///     Controls the page size of the workflow instance that are retained in a single batch
    /// </summary>
    public int PageSize { get; set; } = 25;
}