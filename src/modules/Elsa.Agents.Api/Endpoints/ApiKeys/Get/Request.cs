using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents.Api.Endpoints.ApiKeys.Get;

public class Request
{
    [Required] public string Id { get; set; } = default!;
}