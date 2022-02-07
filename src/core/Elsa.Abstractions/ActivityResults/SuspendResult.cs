using Elsa.Events;
using Elsa.Models;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.ActivityResults
{
    public class SuspendResult : ActivityExecutionResult
    {
        public override async ValueTask ExecuteAsync(ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var activityDefinition = activityExecutionContext.ActivityBlueprint;
            var blockingActivity = new BlockingActivity(activityDefinition.Id, activityDefinition.Type);
            activityExecutionContext.WorkflowExecutionContext.WorkflowInstance.BlockingActivities.Add(blockingActivity);
            activityExecutionContext.WorkflowExecutionContext.Suspend();

            var mediator = activityExecutionContext.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Publish(new BlockingActivityAdded(activityExecutionContext.WorkflowExecutionContext, blockingActivity), cancellationToken);
        }
    }
}