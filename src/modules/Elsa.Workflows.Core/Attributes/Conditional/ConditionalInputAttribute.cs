namespace Elsa.Workflows.Attributes.Conditional;

[AttributeUsage(AttributeTargets.Property)]
public class ConditionalInput : InputAttribute
{
    public ConditionalInput(string[] showForStates) : base()
    {
        InputType = InputType.ConditionalInput;
        ShowForStates = showForStates;
    }
}
