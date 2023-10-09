using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Workflows.Core;

namespace Elsa.IntegrationTests.Scenarios.FlowchartNextActivity.Activities;

public class CustomActivity : CodeActivity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await context.CompleteActivityAsync();
    }
}