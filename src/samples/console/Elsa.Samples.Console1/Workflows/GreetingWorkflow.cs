using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

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