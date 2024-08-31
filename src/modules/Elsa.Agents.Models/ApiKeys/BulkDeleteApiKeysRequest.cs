using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents;

public class BulkDeleteApiKeysRequest
{
    [Required] public ICollection<string> Ids { get; set; } = default!;
}