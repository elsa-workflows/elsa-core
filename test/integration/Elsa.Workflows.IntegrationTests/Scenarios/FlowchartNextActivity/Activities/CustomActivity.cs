using Elsa.Extensions;
using Elsa.Workflows;

namespace Elsa.Workflows.IntegrationTests.Scenarios.FlowchartNextActivity.Activities;

public class CustomActivity : CodeActivity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await context.CompleteActivityAsync();
    }
}