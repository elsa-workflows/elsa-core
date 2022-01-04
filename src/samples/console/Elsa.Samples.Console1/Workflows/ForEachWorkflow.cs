using System.Collections.Generic;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Workflows;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Samples.Console1.Workflows;

public static class ForEachWorkflow 
{
    public static IActivity Create()
    {
        var items = new[] { "C#", "Rust", "Go" };
        var currentItem = new Variable<string>();
            
        return new Sequence
        {
            Activities =
            {
                new WriteLine("Programming languages:"),
                new ForEach<string>
                {
                    Items = new Input<ICollection<string>>(items),
                    CurrentValue = currentItem,
                    Body = new WriteLine(context => currentItem.Get(context))
                },
                new WriteLine("Done.")
            }
        };
    }
}