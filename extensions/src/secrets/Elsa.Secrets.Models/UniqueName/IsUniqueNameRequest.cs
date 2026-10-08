using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Elsa.Secrets.UniqueName;

[UsedImplicitly]
public class IsUniqueNameRequest
{
    [Required] public string Name { get; set; } = null!;
    public string? Id { get; set; }
}