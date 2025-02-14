namespace Elsa.Api.Client.Resources.ActivityDescriptors.Models;

public record ConditionalDescriptor
{
    public string[] ShowForStates { get; set; } = [];
    public InputType InputType{ get; set; }
    public List<string>? DropDownStates { get; set; }

}