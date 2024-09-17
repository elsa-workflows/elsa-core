using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Elsa.Secrets.UniqueName;

[UsedImplicitly]
public class IsUniqueNameRequest
{
    [Required] public string Name { get; set; } = default!;
    public string? Id { get; set; }
}