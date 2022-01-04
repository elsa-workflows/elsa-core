using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Contracts;

namespace Elsa.Management.Models;

public class ActivityDescriptor : NodeDescriptor
{
    [JsonIgnore] public Func<ActivityConstructorContext, IActivity> Constructor { get; init; } = default!;
    public ActivityTraits Traits { get; set; } = ActivityTraits.Action;
    public ICollection<Port> InPorts { get; init; } = new List<Port>();
    public ICollection<Port> OutPorts { get; init; } = new List<Port>();
}

public record ActivityConstructorContext(JsonElement Element, JsonSerializerOptions SerializerOptions);