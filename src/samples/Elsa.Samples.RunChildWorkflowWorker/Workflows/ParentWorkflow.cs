using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Timers;
using Elsa.Activities.Workflows;
using Elsa.Builders;
using NodaTime;

namespace Elsa.Samples.RunChildWorkflowWorker.Workflows
{
    /// <summary>
    /// Delegate the hard work of counting numbers to a child workflow. 
    /// </summary>
    public class ParentWorkflow : IWorkflow
    {
        private const int Count = 3;
        
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WriteLine("This is the parent workflow.")
                .WriteLine("Let's kick off the child workflow.")
                .RunWorkflow<ChildWorkflow>(RunWorkflow.RunWorkflowMode.Blocking, Count)
                .WriteLine("Parent finished.");
        }
    }

    public class ChildWorkflow : IWorkflowV
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .SetVariable("Count", context => (int)context.Input!)
                .WriteLine(context => $"Child workflow counting down from {context.GetVariable<int>("Count")} to 0")
                .For(context => context.GetVariable<int>("Count"), _ => 0,
                    iterate =>
                    {
                        iterate.WriteLine(context => $"{context.Input}");
                    })
                .WriteLine("Done. Back to you, parent workflow!");
        }
    }
}