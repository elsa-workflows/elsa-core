using System;
using Elsa.Builders;

namespace Sample04
{
    using static Console;
    
    public class HelloWorldWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder) => builder.StartWith(() => WriteLine("Hello World!"));
    }
}