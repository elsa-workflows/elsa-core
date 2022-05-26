using System.Collections.Generic;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Samples.Console1.Workflows;

public static class BlockingParallelForEachWorkflow 
{
    public static IActivity Create()
    {
        var items = new[] { "C#", "Rust", "Go" };
        var currentItem = new Variable<string>();
            
        return new Sequence
        {
            Activities =
            {
                new WriteLine("Programming languages in parallel:"),
                new ParallelForEach<string>()
                {
                    Items = new Input<ICollection<string>>(items),
                    CurrentValue = currentItem,
                    Body = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine(context => $"Begin: {currentItem.Get(context)}"),
                            new Event(context => currentItem.Get(context)),
                            new WriteLine(context => $"Completed: {currentItem.Get(context)}")
                        }
                    }
                },
                new WriteLine("Done.")
            }
        };
    }
}