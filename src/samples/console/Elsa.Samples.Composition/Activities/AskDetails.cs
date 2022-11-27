using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;

public class AskDetails : Composite<Person>
{
    private readonly Variable<string> _name = new();
    private readonly Variable<int> _age = new();

    public Input<string> NamePrompt { get; set; } = new("Please tell me your name:");
    public Input<string> AgePrompt { get; set; } = new("Please tell me your age:");

    public AskDetails()
    {
        Root = new Sequence
        {
            Variables = { _name, _age },
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