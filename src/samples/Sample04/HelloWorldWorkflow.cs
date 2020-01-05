using System;
using Elsa.Builders;

namespace Sample04
{
    using static Console;
    
    /// <summary>
    /// A simple workflow with one activity. All Workflows have a single "root" activity. In this example, we added an inline activity that can execute arbitrary C# code.
    /// </summary>
    public class HelloWorldWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder) => builder.StartWith(() => WriteLine("Hello World!"));
    }
}