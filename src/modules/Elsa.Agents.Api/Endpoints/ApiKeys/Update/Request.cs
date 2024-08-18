using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents.Api.Endpoints.ApiKeys.Update;

public class Request
{
    [Required] public string Id { get; set; } = default!;
    [Required] public string Name { get; set; } = default!;
    [Required] public string Value { get; set; }= default!;
}