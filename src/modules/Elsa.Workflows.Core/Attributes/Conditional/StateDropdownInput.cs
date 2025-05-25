using Elsa.Workflows.UIHints;

namespace Elsa.Workflows.Attributes.Conditional;

[AttributeUsage(AttributeTargets.Property)]
public class StateDropdownInput : InputAttribute
{
    public StateDropdownInput(string[] stateOptions) : base()
    {
        UIHint = InputUIHints.DropDown;
        InputType = InputType.StateDropdown;
        Options = stateOptions.ToList();
    }
}
