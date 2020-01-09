using System;
using System.Linq;
using Elsa.Builders;

namespace Sample07
{
    using static Console;
    
    /// <summary>
    /// A simple flow chart where each activity is connected to the next.
    /// </summary>
    public class MyWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .SetVariable("RandomNumber", () => new Random().Next())
                .Then((w, a) => WriteLine($"Your lucky number is: {w.GetVariable<int>("RandomNumber")}"));
        }
    }
}