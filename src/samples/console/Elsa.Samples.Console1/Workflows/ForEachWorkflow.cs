using System.Collections.Generic;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

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
                    CurrentValue = new Output<Variable<string>?>(currentItem),
                    Body = new WriteLine(context => currentItem.Get(context))
                },
                new WriteLine("Done.")
            }
        };
    }
}