using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes.Conditional;
using Elsa.Workflows.Models;

namespace Elsa.Server.Web;

public class CondActivity : CodeActivity
{

    [StateDropdownInput(
        ["double", "string"],
        DefaultValue = "double")]
    public Input<string> ApiName{ get; set; } = default;
    
    [ConditionalInput(["double"], Description = "Double")]
    public Input<double> DoubleInput {get; set;} = default;

    [ConditionalInput(["string"], Description = "String")]
    public Input<string> StringInput {get; set;} = default;

    protected override void Execute(ActivityExecutionContext context)
    {
        object? value = null;
        if (ApiName.Get(context) == "double")
            value = DoubleInput.Get(context);
        else if (ApiName.Get(context) == "string")
            value = StringInput.Get(context);
        Console.WriteLine(value);
    }
}
