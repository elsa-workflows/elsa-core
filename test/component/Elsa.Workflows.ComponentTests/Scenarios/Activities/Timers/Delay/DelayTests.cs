using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers.Delay.Workflows;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers.Delay;

public class DelayTests(App app) : TimerActivityTestBase(app)
{
    [Fact(DisplayName = "Delay activity blocks workflow execution and resumes after specified duration")]
    public async Task DelayActivity_BlocksAndResumes()
    {
        await AssertActivityBlocksAndResumes(DelayWorkflow.DefinitionId);
    }
}
