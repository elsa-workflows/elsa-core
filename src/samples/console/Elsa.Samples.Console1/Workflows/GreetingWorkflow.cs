using Elsa.Activities.Console;
using Elsa.Activities.Workflows;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Samples.Console1.Workflows;

public static class GreetingWorkflow
{
    public static IActivity Create()
    {
        var name = new Variable<string>();

        return new Sequence
        {
            Variables = { name },
            Activities =
            {
                new WriteLine("What's your name?"),
                new ReadLine(name),
                new WriteLine(context => $"Nice to meet you, {name.Get(context)}!")
            }
        };
    }
}