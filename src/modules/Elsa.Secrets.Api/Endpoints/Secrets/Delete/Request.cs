using System.ComponentModel.DataAnnotations;

namespace Elsa.Secrets.Api.Endpoints.Secrets.Delete;

public class Request
{
    [Required] public string Id { get; set; } = default!;
}