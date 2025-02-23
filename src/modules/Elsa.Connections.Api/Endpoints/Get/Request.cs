using System.ComponentModel.DataAnnotations;

namespace Elsa.Connections.Api.Endpoints.Get;

public class Request
{
    [Required] public string Id { get; set; } = null!;
}