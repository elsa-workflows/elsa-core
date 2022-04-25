using Elsa.Activities;
using Elsa.Modules.Activities.Console;
using Elsa.Services;

namespace Elsa.IntegrationTests.Activities;

class NestedSequentialWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new WriteLine("Start"),
                new Sequence
                {
                    Activities =
                    {
                        new WriteLine("Line 1"),
                        new WriteLine("Line 2"),
                        new WriteLine("Line 3")
                    }
                },
                new WriteLine("End"),
            }
        });
    }
}