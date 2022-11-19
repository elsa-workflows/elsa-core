using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;

public class AskAge : Composite<int>
{
    private readonly WriteLine _prompt = new();
    private readonly Variable<string> _age = new();

    public Input<string> Prompt { get; set; } = new("Please tell me your age:");

    public AskAge()
    {
        Root = new Sequence
        {
            Variables = { _age },
            Activities =
            {
                _prompt,
                new ReadLine(_age)
            }
        };
    }

    protected override void ConfigureActivities(ActivityExecutionContext context)
    {
        _prompt.Text = Prompt;
    }

    protected override void OnCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        var age = _age.Get<int>(context);
        context.Set(Result, age);
    }
}