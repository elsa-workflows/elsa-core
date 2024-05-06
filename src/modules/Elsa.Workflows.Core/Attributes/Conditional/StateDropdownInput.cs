using Elsa.Workflows.Attributes.Conditional;
using Elsa.Workflows.UIHints;
using Newtonsoft.Json;
using Workflows.Activities.Shared.Inputs.Generics;

[AttributeUsage(AttributeTargets.Property)]
public class StateDropdownInput : Input
{
    public List<string> Ids { get; set; } = new();
    public StateDropdownInput(string[] states) : base()
    {
        Init();
        ParseDynamicStates(states);
        Description = JsonConvert.SerializeObject(new
        {
            Mode = "Dynamic",
            InputType = InputType,
            Description = UIDescription,
            Ids,
            Options
        });
    }

    private void Init()
    {
        UIHint = InputUIHints.DropDown;
        InputType = "StateDropdown";
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
        UIDescription = descriptions;
        Ids = ids;
    }
}
