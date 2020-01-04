using System;
using Elsa.Builders;

namespace Sample06
{
    using static Console;
    
    public class HelloWorldWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithId("MyWorkflow")
                .StartWith(() => WriteLine("Hello World!"));
        }
    }
}