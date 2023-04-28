using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Samples.WorkflowServer.Activities;

/// <summary>
/// This activity shows how to provide default values for inputs.
/// </summary>
[Activity("Demo", "Demo", Description = "A demo activity with default settings.")]
public class MyActivity : CodeActivity
{
    [Input(DefaultValue = "Hello World!")] public Input<string> Message { get; set; } = default!;

    [Input(DefaultValue = true)] public Input<bool> Toggle { get; set; } = default!;

    [Input(DefaultValue = "B", Options = new[] { "A", "B", "C" }, UIHint = InputUIHints.Dropdown)]
    public Input<string> SelectedOption { get; set; } = default!;

    [Input(DefaultValue = new[] { "C#", "Programming" }, UIHint = InputUIHints.MultiText)]
    public Input<string[]> Tags { get; set; } = default!;

    [Input(DefaultValue = "B", Options = new[] { "A", "B", "C" }, UIHint = InputUIHints.RadioList)]
    public Input<string> SelectedRadioOption { get; set; } = default!;

    [Input(DefaultValue = new[] { "A", "C" }, Options = new[] { "A", "B", "C" }, UIHint = InputUIHints.CheckList)]
    public Input<string> SelectedCheckOption { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var message = Message.Get(context);
        var toggle = Toggle.Get(context);
        var selectedOption = SelectedOption.Get(context);
        var tags = Tags.Get(context);
        var selectedRadioOption = SelectedRadioOption.Get(context);
        var selectedCheckOption = SelectedCheckOption.Get(context);

        Console.WriteLine(message);
        Console.WriteLine("Toggle: {0}", toggle);
        Console.WriteLine("Selected option: {0}", selectedOption);
        Console.WriteLine("Tags: {0}", string.Join(", ", tags));
        Console.WriteLine("Selected radio option: {0}", selectedRadioOption);
        Console.WriteLine("Selected check option: {0}", string.Join(", ", selectedCheckOption));
    }
}