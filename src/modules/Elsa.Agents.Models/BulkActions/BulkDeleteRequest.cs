using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents;

public class BulkDeleteRequest
{
    [Required] public ICollection<string> Ids { get; set; } = default!;
}