using System.ComponentModel.DataAnnotations;

namespace Elsa.Connections.Api.Endpoints.Delete;

public class Request
{
    [Required] public string Id { get; set; } = null!;
}