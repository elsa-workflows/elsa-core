namespace Elsa.MassTransit.Contracts;

/// <summary>
/// Represents a contract for formatting channel queue names.
/// </summary>
public interface IEndpointChannelFormatter
{
    /// <summary>
    /// Formats the queue name based on the provided channel name.
    /// </summary>
    /// <param name="channelName">The channel name to be formatted.</param>
    /// <returns>The formatted queue name.</returns>
    string FormatEndpointName(string? channelName = default);
}