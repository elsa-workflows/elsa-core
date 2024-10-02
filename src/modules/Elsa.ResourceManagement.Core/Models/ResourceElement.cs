using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.ResourceManagement.Contracts;

namespace Elsa.ResourceManagement;

public class ResourceElement : IResource
{
    private Dictionary<string, ResourceElement> _elements = default!;
    //private JsonDynamicObject _dynamicObject;
    private JsonObject _data = default!;

    protected ResourceElement() : this([])
    {
    }

    protected ResourceElement(JsonObject data) => Data = data;

    [JsonIgnore]
    protected internal Dictionary<string, ResourceElement> Elements => _elements ??= [];

    // [JsonIgnore]
    // public dynamic Content => _dynamicObject ??= Data;

    [JsonIgnore]
    internal JsonObject Data
    {
        get => _data;
        set
        {
            //_dynamicObject = null;
            _data = value;
        }
    }

    [JsonIgnore]
    public ResourceItem ResourceItem { get; set; } = default!;

    /// <summary>
    /// Whether the resource has a named property or not.
    /// </summary>
    /// <param name="name">The name of the property to look for.</param>
    public bool Has(string name) => Data.ContainsKey(name);
}