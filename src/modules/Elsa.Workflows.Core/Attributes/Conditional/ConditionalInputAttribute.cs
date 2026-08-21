using Elsa.Workflows.Attributes.Conditional;
using Newtonsoft.Json;


namespace Elsa.Workflows.Attributes.Conditional;

[AttributeUsage(AttributeTargets.Property)]
public class ConditionalInput : Input
{
    public ConditionalInput(string[] showForStates, string description = "") : base([description])
    {
        InputType = "ConditionalInput";
        ShowForStates = showForStates;
        Description = JsonConvert.SerializeObject(
            new
            {
                InputType = InputType,
                Description = UIDescription,
                ShowForStates
            }
        );
    }
    public string[] ShowForStates { get; set; } = ["Default"];
}
