using System;
using Elsa.Builders;

namespace Sample05
{
    using static Console;
    
    /// <summary>
    /// A simple workflow with one activity.
    /// </summary>
    public class HelloWorldWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder) => builder.StartWith(() => WriteLine("Hello World!"));
    }
}