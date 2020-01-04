using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class Scheduler : IScheduler
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IExpressionEvaluator expressionEvaluator;
        private readonly IIdGenerator idGenerator;

        public Scheduler(
            IExpressionEvaluator expressionEvaluator,
            IIdGenerator idGenerator,
            IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.expressionEvaluator = expressionEvaluator;
            this.idGenerator = idGenerator;
        }

        public async Task<WorkflowExecutionContext> ScheduleActivityAsync(
            IActivity activity,
            object? input = default,
            CancellationToken cancellationToken = default)
        {
            var instanceId = idGenerator.Generate();
            var workflowExecutionContext = new WorkflowExecutionContext(expressionEvaluator, serviceProvider, instanceId);
            
            workflowExecutionContext.ScheduleActivity(activity, input);
            
            while (workflowExecutionContext.HasScheduledActivities)
            {
                var scheduledActivity = workflowExecutionContext.PopScheduledActivity();
                var currentActivity = scheduledActivity.Activity;
                var activityExecutionContext = new ActivityExecutionContext(scheduledActivity.Activity, scheduledActivity.Input);
                var result = await currentActivity.ExecuteAsync(workflowExecutionContext, activityExecutionContext, cancellationToken);

                await result.ExecuteAsync(workflowExecutionContext, activityExecutionContext, cancellationToken);
            }

            return workflowExecutionContext;
        }
    }
}