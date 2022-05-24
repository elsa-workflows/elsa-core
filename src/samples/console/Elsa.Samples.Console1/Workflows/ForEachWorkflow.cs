using System.Collections.Generic;
using Elsa.Activities;
using Elsa.Models;
using Elsa.Services;

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