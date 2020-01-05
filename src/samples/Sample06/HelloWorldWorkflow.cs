using System;
using Elsa.Builders;

namespace Sample06
{
    using static Console;
    
    /// <summary>
    /// A simple workflow with one activity.
    /// </summary>
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