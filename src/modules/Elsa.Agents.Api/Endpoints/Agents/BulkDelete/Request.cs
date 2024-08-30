using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents.Api.Endpoints.Agents.BulkDelete;

public class Request
{
    [Required] public ICollection<string> Ids { get; set; } = default!;
}