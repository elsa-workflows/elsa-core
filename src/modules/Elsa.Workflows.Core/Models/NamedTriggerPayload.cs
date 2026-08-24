namespace Elsa.Workflows.Models;

/// <summary>
/// A trigger payload that carries its own stimulus name.
/// </summary>
/// <remarks>
/// Return instances of this type from <see cref="ITrigger.GetTriggerPayloadsAsync"/> when a single trigger needs to register payloads under
/// more than one stimulus name. The wrapper is unwrapped during indexing: the stored trigger's name and hash are derived from <see cref="Name"/>
/// and its payload from <see cref="Payload"/>. Payloads that are not wrapped are named using <see cref="TriggerIndexingContext.TriggerName"/>.
/// </remarks>
public record NamedTriggerPayload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NamedTriggerPayload"/> class.
    /// </summary>
    /// <param name="name">The stimulus name to register the payload under.</param>
    /// <param name="payload">The payload to register.</param>
    public NamedTriggerPayload(string name, object payload)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A stimulus name is required.", nameof(name));

        if (payload is NamedTriggerPayload)
            throw new ArgumentException("A NamedTriggerPayload cannot wrap another NamedTriggerPayload.", nameof(payload));

        Name = name;
        Payload = payload;
    }

    /// <summary>
    /// The stimulus name to register the payload under. Publishers of this stimulus compute their hash from this same name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The payload to register. This is what gets stored; the wrapper itself never is, since the constructor refuses to wrap another
    /// <see cref="NamedTriggerPayload"/>.
    /// </summary>
    public object Payload { get; }
}
