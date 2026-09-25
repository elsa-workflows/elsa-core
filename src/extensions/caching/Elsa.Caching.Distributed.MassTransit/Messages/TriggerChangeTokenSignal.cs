namespace Elsa.Caching.Distributed.MassTransit.Messages;

/// <summary>
/// Represents a message containing a signal to trigger a change token.
/// </summary>
/// <param name="Key">The key of the change token to trigger.</param>
public record TriggerChangeTokenSignal(string Key);