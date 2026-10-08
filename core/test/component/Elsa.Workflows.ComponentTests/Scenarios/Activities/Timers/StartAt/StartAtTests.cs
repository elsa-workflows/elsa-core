using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers.StartAt.Workflows;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers.StartAt;

public class StartAtTests(App app) : TimerActivityTestBase(app)
{
    [Fact(DisplayName = "StartAt activity blocks workflow execution and resumes at specified time")]
    public async Task StartAtActivity_BlocksAndResumes()
    {
        await AssertActivityBlocksAndResumes(StartAtWorkflow.DefinitionId);
    }
}
