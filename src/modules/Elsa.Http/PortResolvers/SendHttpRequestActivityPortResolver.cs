using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;

namespace Elsa.Http.PortResolvers;

/// <summary>
/// Returns a list of outbound activities for a given <see cref="SendHttpRequest"/> activity's expected status codes.
/// </summary>
public class SendHttpRequestActivityResolver : IActivityResolver
{
    /// <inheritdoc />
    public int Priority => 0;

    /// <inheritdoc />
    public bool GetSupportsActivity(IActivity activity) => activity is SendHttpRequest;


    /// <inheritdoc />
    public ValueTask<IEnumerable<ActivityPort>> GetActivityPortsAsync(IActivity activity, CancellationToken cancellationToken = default)
    {
        IEnumerable<ActivityPort> ports = GetPortsInternal(activity);
        return new ValueTask<IEnumerable<ActivityPort>>(ports);
    }

    private IEnumerable<ActivityPort> GetPortsInternal(IActivity activity)
    {
        var sendHttpRequest = (SendHttpRequest)activity;
        var cases = sendHttpRequest.ExpectedStatusCodes.Where(x => x.Activity != null);

        foreach (var @case in cases)
            yield return ActivityPort.FromActivity(@case.Activity!, @case.StatusCode.ToString());

        if (sendHttpRequest.UnmatchedStatusCode != null)
            yield return ActivityPort.FromActivity(sendHttpRequest.UnmatchedStatusCode, nameof(SendHttpRequest.ExpectedStatusCodes));
    }
}