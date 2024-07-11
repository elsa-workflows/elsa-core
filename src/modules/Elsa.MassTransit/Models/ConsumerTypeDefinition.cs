namespace Elsa.MassTransit.Models;

/// <summary>
/// Represents the definition of a consumer type.
/// </summary>
public record ConsumerTypeDefinition(
    Type ConsumerType,
    Type? ConsumerDefinitionType = default,
    string? Name = null,
    bool IsTemporary = false,
    bool IgnoreConsumersDisabled = false);