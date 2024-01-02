namespace Elsa.MassTransit.Models;

internal record ConsumerTypeDefinition(Type ConsumerType, Type? ConsumerDefinitionType = default);