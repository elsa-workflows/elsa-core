using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Samples.AspNet.ChildWorkflows.Workflows;

public class ParentWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var childOutput = builder.WithVariable<IDictionary<string, object>>();

        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("I am the parent workflow."),
                new DispatchWorkflow
                {
                    WorkflowDefinitionId = new(nameof(ChildWorkflow)),
                    Input = new(new Dictionary<string, object>
                    {
                        ["ParentMessage"] = "Hello from parent!"
                    }),
                    WaitForCompletion = new(true),
                    Result = new(childOutput)
                },
                new WriteLine(context => $"Child finished executing and said: {childOutput.Get(context)!["ChildMessage"]}")
            }
        };
    }
}