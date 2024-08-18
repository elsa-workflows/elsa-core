using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents.Api.Endpoints.Services.Create;

public class Request
{
    [Required] public string Name { get; set; } = default!;
    [Required] public string Type { get; set; } = default!;
    public IDictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
}