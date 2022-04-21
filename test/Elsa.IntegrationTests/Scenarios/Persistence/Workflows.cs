using Elsa.Activities;
using Elsa.Contracts;
using Elsa.Modules.Activities.Console;
using Elsa.Modules.Activities.Primitives;

namespace Elsa.IntegrationTests.Scenarios.Persistence;

class BlockingSequentialWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new WriteLine("Line 1"),
                new Event("Resume"){ Id = "Resume"},
                new WriteLine("Line 2"),
                new WriteLine("Line 3")
            }
        });
    }
}