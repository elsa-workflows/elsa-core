using System.Text.Json;

namespace Elsa.Expressions.Models;

public record ExpressionSerializationContext(JsonElement JsonElement, JsonSerializerOptions Options, Type MemoryBlockType);