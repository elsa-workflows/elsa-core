using System.ComponentModel.DataAnnotations;

namespace Elsa.Secrets.Api.Endpoints.Secrets.Get;

public class Request
{
    [Required] public string Id { get; set; } = default!;
}