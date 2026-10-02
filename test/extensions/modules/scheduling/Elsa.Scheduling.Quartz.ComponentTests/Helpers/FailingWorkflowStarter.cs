using Elsa.Workflows.Runtime;

namespace Elsa.Scheduling.Quartz.ComponentTests.Helpers;

/// <summary>
/// A workflow starter that can be configured to fail with transient or non-transient exceptions.
/// </summary>
public class FailingWorkflowStarter(IWorkflowStarter innerStarter) : IWorkflowStarter
{
    private int _callCount;

    public int FailuresBeforeSuccess { get; set; }
    public Exception? ExceptionToThrow { get; set; }

    /// <summary>
    /// The response to return once the configured number of failures has been exhausted. When not set, the call is
    /// forwarded to the inner starter.
    /// </summary>
    public StartWorkflowResponse? SuccessResponse { get; set; }

    public int CallCount => _callCount;

    public async Task<StartWorkflowResponse> StartWorkflowAsync(StartWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        _callCount++;

        if (ExceptionToThrow != null && _callCount <= FailuresBeforeSuccess)
        {
            throw ExceptionToThrow;
        }

        if (SuccessResponse != null)
            return SuccessResponse;

        return await innerStarter.StartWorkflowAsync(request, cancellationToken);
    }

    public void Reset()
    {
        _callCount = 0;
    }
}
