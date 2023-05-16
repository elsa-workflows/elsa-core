using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Activities.SetOutput;

public class AskDetails : Composite<Person>
{
    private readonly Variable<string> _name = new();
    private readonly Variable<int> _age = new();

    public Input<string> NamePrompt { get; set; } = new("Please tell me your name:");
    public Input<string> AgePrompt { get; set; } = new("Please tell me your age:");

    public AskDetails()
    {
        Variables = new List<Variable> { _name, _age };
        Root = new Sequence
        {
            Activities =
            {
                new AskName
                {
                    Prompt = new (context => NamePrompt.Get(context)),
                    Result = new (_name)
                },
                new AskAge
                {
                    Prompt = new (context => AgePrompt.Get(context)),
                    Result = new (_age)
                }
            }
        };
    }

    protected override void OnCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        var name = _name.Get<string>(context)!;
        var age = _age.Get<int>(context);
        var person = new Person(name, age);

        context.Set(Result, person);
    }
}