using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents;

public class ApiKeyInputModel
{
    [Required] public string Name { get; set; } = null!;
    [Required] public string Value { get; set; } = null!;
}