using Elsa.Workflows.Core.Contracts;

namespace Elsa.Http.PortResolvers;

/// <summary>
/// Returns a list of outbound activities for a given <see cref="SendHttpRequest"/> activity's expected status codes.
/// </summary>
public class SendHttpRequestActivityPortResolver : IActivityPortResolver
{
    /// <inheritdoc />
    public int Priority => 0;

    /// <inheritdoc />
    public bool GetSupportsActivity(IActivity activity) => activity is SendHttpRequest;

    /// <inheritdoc />
    public ValueTask<IEnumerable<IActivity>> GetPortsAsync(IActivity activity, CancellationToken cancellationToken = default)
    {
        var ports = GetPortsInternal(activity);
        return new(ports);
    }

    private IEnumerable<IActivity> GetPortsInternal(IActivity activity)
    {
        var sendHttpRequest = (SendHttpRequest)activity;
        var cases = sendHttpRequest.ExpectedStatusCodes.Where(x => x.Activity != null);

        foreach (var @case in cases)
            yield return @case.Activity!;

        if (sendHttpRequest.UnmatchedStatusCode != null)
            yield return sendHttpRequest.UnmatchedStatusCode;
    }
}