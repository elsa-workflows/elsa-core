using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents.Api.Endpoints.Agents.Get;

public class Request
{
    [Required] public string Id { get; set; } = default!;
}