using System.ComponentModel.DataAnnotations;

namespace Elsa.Secrets.BulkActions;

public class BulkDeleteRequest
{
    [Required] public ICollection<string> Ids { get; set; } = default!;
}