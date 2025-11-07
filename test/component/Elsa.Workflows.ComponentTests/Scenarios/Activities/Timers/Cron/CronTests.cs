using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers.Cron.Workflows;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers.Cron;

public class CronTests(App app) : TimerActivityTestBase(app)
{
    [Fact(DisplayName = "Cron activity blocks workflow execution and resumes at next cron occurrence")]
    public async Task CronActivity_BlocksAndResumes()
    {
        await AssertActivityBlocksAndResumes(CronWorkflow.DefinitionId);
    }
}
