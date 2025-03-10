using Elsa.Workflows.Attributes;

namespace Elsa.Workflows.Models;

public record ConditionalDescriptor
{
    public string[] ShowForStates { get; set; } = [];
    public InputType InputType{ get; set; }
    public List<string>? DropDownStates { get; set; }

}