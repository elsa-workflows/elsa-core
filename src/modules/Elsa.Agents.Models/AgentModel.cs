using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents;

public class AgentModel : AgentInputModel
{
    [Required] public string Id { get; set; } = default!;
}