namespace Elsa.MassTransit.Models;

public record ConsumerTypeDefinition(Type ConsumerType, Type? ConsumerDefinitionType = default, string? Name = null, bool IsTemporary = false);