using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents;

public class ServiceInputModel
{
    [Required] public string Name { get; set; } = default!;
    [Required] public string Type { get; set; } = default!;
    public IDictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
}