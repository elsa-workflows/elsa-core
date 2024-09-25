using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents;

public class ApiKeyInputModel
{
    [Required] public string Name { get; set; } = default!;
    [Required] public string Value { get; set; } = default!;
}