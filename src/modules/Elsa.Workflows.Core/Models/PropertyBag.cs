using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.Workflows.Core.Models;

[JsonConverter(typeof(PropertyBagConverter))]
public class PropertyBag
{
    [JsonConstructor]
    public PropertyBag() : this(new Dictionary<string, object>())
    {
    }

    public PropertyBag(IDictionary<string, object> properties)
    {
        Properties = properties;
    }
    
    public IDictionary<string, object> Properties { get; init; }
}