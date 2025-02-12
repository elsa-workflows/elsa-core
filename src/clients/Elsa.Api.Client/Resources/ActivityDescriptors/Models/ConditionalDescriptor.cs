namespace Elsa.Api.Client.Resources.ActivityDescriptors.Models;

public record ConditionalDescriptor
{
    public string[] ShowForStates { get; set; } = [];
    public InputType InputType{ get; set; }
    public DropDownStates? DropDownStates { get; set; }

}

public record DropDownStates {
    public string Mode {get; set;} = "Dynamic";
    public required List<string> Descriptions {get; set;}
    public required List<string> Ids { get; set; }
    public required List<string> Options { get; set; }
}