using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents.Api.Endpoints.Services.Delete;

public class Request
{
    [Required] public string Id { get; set; } = default!;
}