using System.Text.Json.Serialization;

namespace Elsa.Connections.Contracts;
interface IConnectionProperty
{
    public string ConnectionName { get; set; }

    [JsonIgnore]
    public object Properties { get; set; }
}

