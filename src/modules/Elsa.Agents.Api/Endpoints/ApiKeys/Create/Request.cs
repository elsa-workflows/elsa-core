using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents.Api.Endpoints.ApiKeys.Create;

public class Request
{
    [Required] public string Name { get; set; } = default!;
    [Required] public string Value { get; set; }= default!;
}