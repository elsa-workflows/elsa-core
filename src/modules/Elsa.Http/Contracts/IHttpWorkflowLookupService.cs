namespace Elsa.Http;

/// <summary>
/// Represents a service that looks up HTTP workflows and triggers.
/// </summary>
public interface IHttpWorkflowLookupService
{
    /// <summary>
    /// Finds a workflow and trigger by bookmark hash.
    /// </summary>
    Task<HttpWorkflowLookupResult?> FindWorkflowAsync(string bookmarkHash, CancellationToken cancellationToken = default);
}