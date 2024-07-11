using Elsa.MassTransit.Contracts;
using Humanizer;

namespace Elsa.MassTransit.Formatters;

/// <summary>
/// The DefaultChannelQueueFormatter class is an implementation of the IChannelQueueFormatter interface.
/// It is responsible for formatting channel queue names.
/// </summary>
public class DefaultEndpointChannelFormatter : IEndpointChannelFormatter
{
    /// <inheritdoc />
    public string FormatEndpointName(string? channelName)
    {
        var channelSuffix = !string.IsNullOrEmpty(channelName) ? "-" + channelName.Kebaberize() : "";
        return $"elsa-dispatch-workflow-request{channelSuffix}";
    }
}