using Elsa.Api.Client.Resources.Alterations.Models;
using Elsa.Api.Client.Resources.Alterations.Requests;
using Elsa.Api.Client.Resources.Alterations.Responses;
using Refit;

namespace Elsa.Api.Client.Resources.Alterations.Contracts;

/// <summary>
/// Represents a client for the alterations API. Requires the Elsa.Alterations feature.
/// </summary>
public interface IAlterationsApi
{
    /// <summary>
    /// Returns an alteration plan and its associated jobs.
    /// </summary>
    /// <param name="id">The ID of the alteration plan to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Get("/alterations/{id}")]
    Task<GetAlterationPlanResponse> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Determines which workflow instances a "Submit" request would target without actually running an alteration
    /// </summary>
    /// <param name="request">The requested workflow filter to dry run</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Post("/alterations/dry-run")]
    Task<DryRunResponse> DryRun(AlterationWorkflowInstanceFilter request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits an alteration plan and a filter for workflows instances to be executed against
    /// </summary>
    /// <param name="request">The alterations and filter to submit</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Post("/alterations/submit")]
    Task<SubmitResponse> Submit(AlterationPlanParams request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs an alteration plan and a list of workflow Instance Ids to be executed against
    /// </summary>
    /// <param name="request">The alterations and workflowInstanceIds to execute</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Post("/alterations/run")]
    Task<RunResponse> Run(RunRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retries the specified workflow instances.
    /// </summary>
    /// <param name="request">The request containing the selection of workflow instances to retry.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Post("/alterations/workflows/retry")]
    Task<BulkRetryResponse> BulkRetryAsync(BulkRetryRequest request, CancellationToken cancellationToken);
}