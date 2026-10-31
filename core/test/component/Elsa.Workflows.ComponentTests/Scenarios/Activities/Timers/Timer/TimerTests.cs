using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers.Timer.Workflows;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers.Timer;

public class TimerTests(App app) : TimerActivityTestBase(app)
{
    [Fact(DisplayName = "Timer activity blocks workflow execution and resumes after specified interval")]
    public async Task TimerActivity_BlocksAndResumes()
    {
        await AssertActivityBlocksAndResumes(TimerWorkflow.DefinitionId);
    }
}
