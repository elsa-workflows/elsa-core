using Elsa.Extensions;
using Elsa.Samples.ConsoleApp.Composition.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;

namespace Elsa.Samples.ConsoleApp.Composition.Activities;

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

    protected override void OnCompleted(ActivityCompletedContext context)
    {
        var name = _name.Get<string>(context.TargetContext)!;
        var age = _age.Get<int>(context.TargetContext);
        var person = new Person(name, age);

        context.TargetContext.Set(Result, person);
    }
}