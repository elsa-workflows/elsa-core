using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;

public class AskAge : Composite<int>
{
    private readonly Variable<string> _age = new();

    public Input<string> Prompt { get; set; } = new("Please tell me your age:");

    public AskAge()
    {
        Variables = new List<Variable> { _age };
        Root = new Sequence
        {
            Activities =
            {
                new WriteLine(context => Prompt.Get(context)),
                new ReadLine(_age)
            }
        };
    }

    protected override void OnCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        var age = _age.Get<int>(context);
        context.Set(Result, age);
    }
}