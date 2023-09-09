using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Activities.SetOutput;

namespace Elsa.Samples.AspNet.ChildWorkflows.Workflows;

public class ChildWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine(context => $"I am the child workflow. My parent said: \"{context.GetInput<string>("ParentMessage")}\"."),
                new WriteLine("Let's say hi to the parent!"),
                new SetOutput
                {
                    OutputName = new("ChildMessage"),
                    OutputValue = new("Hello from child!")
                }
            }
        };
    }
}