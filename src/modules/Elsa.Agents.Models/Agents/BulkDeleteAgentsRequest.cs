using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents;

public class BulkDeleteAgentsRequest
{
    [Required] public ICollection<string> Ids { get; set; } = default!;
}