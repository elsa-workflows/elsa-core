using Refit;

namespace Elsa.Api.Client.Resources.Tests;

/// <summary>
/// Represents a client for the testing API.
/// </summary>
public interface ITestsApi
{
    /// <summary>
    /// Sends the specified request to the login API.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    [Post("/tests/activities")]
    Task<TestActivityResponse> TestActivityAsync([Body] TestActivityRequest request, CancellationToken cancellationToken = default);
}