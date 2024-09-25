using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents;

public class ApiKeyModel : ApiKeyInputModel
{
    [Required] public string Id { get; set; } = default!;
}