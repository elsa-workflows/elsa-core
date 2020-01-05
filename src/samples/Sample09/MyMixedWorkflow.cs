using System;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Primitives;
using Elsa.Builders;

namespace Sample09
{
    using static Console;
    
    public class MyMixedWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder.BuildSequence()
                .Add(() => WriteLine("Outer sequence"))
                .Add(x => x.BuildFlowchart()
                    .StartWith(() => WriteLine("Flowchart"))
                    .Then(s => s.BuildSequence()
                        .Add(() => WriteLine("Inner sequence"))
                        .Add(() => WriteLine("Enter your name:"))
                        .Add<ReadLine>()
                        .Add((w, a) => WriteLine($"Nice to meet you, {a.Input.Value}!")))
                    .Then(() => WriteLine("Back in flowchart")))
                .Add(() => WriteLine("Back in outer sequence"))
                .Add(() => WriteLine("Final step"));
        }
    }
}