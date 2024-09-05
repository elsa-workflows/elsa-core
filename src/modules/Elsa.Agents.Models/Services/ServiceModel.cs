using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents;

public class ServiceModel : ServiceInputModel
{
    [Required] public string Id { get; set; } = default!;
}