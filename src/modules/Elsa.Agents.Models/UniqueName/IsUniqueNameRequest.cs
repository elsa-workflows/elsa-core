using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Elsa.Agents;

[UsedImplicitly]
public class IsUniqueNameRequest
{
    [Required] public string Name { get; set; } = default!;
    public string? Id { get; set; }
}