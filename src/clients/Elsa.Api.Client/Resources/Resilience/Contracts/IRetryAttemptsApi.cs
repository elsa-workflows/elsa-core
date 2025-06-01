using Elsa.Api.Client.Resources.Resilience.Models;
using Elsa.Api.Client.Shared.Models;
using Refit;

namespace Elsa.Api.Client.Resources.Resilience.Contracts;

public interface IRetryAttemptsApi
{
    [Get("/resilience/retries/{activityInstanceId}")]
    Task<PagedListResponse<RetryAttemptRecord>> ListAsync(string activityInstanceId, int? skip = null, int? take = null, CancellationToken cancellationToken = default);
}