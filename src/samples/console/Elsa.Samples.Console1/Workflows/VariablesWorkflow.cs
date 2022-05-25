using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Samples.Console1.Workflows;

public static class VariablesWorkflow
{
    public static IActivity Create()
    {
        var greeting = new Variable<string>("Hello! What is your name?");
        var name = new Variable<string>();

        return new Sequence
        {
            Variables = { greeting, name },
            Activities =
            {
                new WriteLine(context => greeting.Get(context)),
                new ReadLine(name),
                new WriteLine(new DelegateReference(context => $"Nice to meet you, {name.Get(context)}!")),
            }
        };
    }
}