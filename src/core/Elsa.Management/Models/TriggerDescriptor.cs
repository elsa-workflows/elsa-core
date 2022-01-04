using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Contracts;

namespace Elsa.Management.Models;

public class TriggerDescriptor : NodeDescriptor
{
    [JsonIgnore] public Func<TriggerConstructorContext, ITrigger> Constructor { get; init; } = default!;
}

public record TriggerConstructorContext(JsonElement Element, JsonSerializerOptions SerializerOptions);