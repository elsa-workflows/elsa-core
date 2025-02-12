using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;

namespace Elsa.Workflows.Attributes.Conditional;

[AttributeUsage(AttributeTargets.Property)]
public class StateDropdownInput : InputAttribute
{
    public StateDropdownInput(string[] states) : base()
    {
        UIHint = InputUIHints.DropDown;
        InputType = InputType.StateDropdown;
        ParseDynamicStates(states);
    }

    public void ParseDynamicStates(string[] states)
    {
        int count = states.Length;
        List<string> options = new();
        List<string> descriptions = new();
        List<string> ids = new();

        for (int i = 0; i < count; ++i)
        {
            string current = states[i];
            if (current == ConditionalInputOptions.WithoutDescription)
            {
                ++i;
                options.Add(states[i]);
                descriptions.Add("");
                ids.Add(states[i]);
            }
            else if (current == ConditionalInputOptions.WithDescription)
            {
                ++i;
                options.Add(states[i]);
                ids.Add(states[i]);
                ++i;
                descriptions.Add(states[i]);
            }
            else if (current == ConditionalInputOptions.WithIdAndDescription)
            {
                ++i;
                options.Add(states[i]);
                ++i;
                ids.Add(states[i]);
                ++i;
                descriptions.Add(states[i]);
            }
        }

        Options = options;
        DropDownStates = new DropDownStates{
            Descriptions = descriptions,
            Ids = ids,
            Options = options
        };
    }
}
