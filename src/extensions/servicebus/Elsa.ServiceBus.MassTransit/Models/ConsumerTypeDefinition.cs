namespace Elsa.ServiceBus.MassTransit.Models;

/// <summary>
/// Represents the definition of a consumer type.
/// </summary>
public record ConsumerTypeDefinition(
    Type ConsumerType,
    Type? ConsumerDefinitionType = null,
    string? Name = null,
    bool IsTemporary = false,
    bool IgnoreConsumersDisabled = false);