using System.Collections.Generic;
using System.Linq;
using Elsa.Comparers;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace Elsa.Extensions
{
    public static class WorkflowExecutionContextExtensions
    {
        public static IEnumerable<IActivity> GetStartActivities(this WorkflowExecutionContext workflowExecutionContext)
        {
            var targetActivities = workflowExecutionContext.Connections.Select(x => x.Target.Activity).Distinct().ToLookup(x => x);

            var query =
                from activity in workflowExecutionContext.Activities
                where !targetActivities.Contains(activity)
                select activity;

            return query;
        }

        public static IActivity GetActivity(this WorkflowExecutionContext workflowExecutionContext, string id) => workflowExecutionContext.Activities.FirstOrDefault(x => x.Id == id);

        public static WorkflowInstance CreateWorkflowInstance(this WorkflowExecutionContext workflowExecutionContext)
        {
            return workflowExecutionContext.UpdateWorkflowInstance(new WorkflowInstance
            {
                Id = workflowExecutionContext.InstanceId,
                DefinitionId = workflowExecutionContext.DefinitionId,
                Version = workflowExecutionContext.Version,
                CreatedAt = workflowExecutionContext.ServiceProvider.GetRequiredService<IClock>().GetCurrentInstant()
            });
        }
        
        public static WorkflowInstance UpdateWorkflowInstance(this WorkflowExecutionContext workflowExecutionContext, WorkflowInstance workflowInstance)
        {
            workflowInstance.Variables = workflowExecutionContext.Variables;
            workflowInstance.ScheduledActivities = new Stack<Models.ScheduledActivity>(workflowExecutionContext.ScheduledActivities.Select(x => new Models.ScheduledActivity(x.Activity.Id, x.Input)));
            workflowInstance.BlockingActivities = new HashSet<BlockingActivity>(workflowExecutionContext.BlockingActivities.Select(x => new BlockingActivity(x.Id, x.Type)), new BlockingActivityEqualityComparer());
            workflowInstance.Status = workflowExecutionContext.Status;
            workflowInstance.CorrelationId = workflowExecutionContext.CorrelationId;
            workflowInstance.Output = workflowExecutionContext.Output;
            workflowInstance.ExecutionLog = workflowExecutionContext.ExecutionLog.Select(x => new Models.ExecutionLogEntry(x.Activity.Id, x.Timestamp)).ToList();

            if (workflowExecutionContext.WorkflowFault != null)
            {
                workflowInstance.Fault = new Models.WorkflowFault
                {
                    FaultedActivityId = workflowExecutionContext.WorkflowFault.FaultedActivity?.Id,
                    Message = workflowExecutionContext.WorkflowFault.Message
                };
            }
            
            return workflowInstance;
        }
        
        public static IEnumerable<Connection> GetInboundConnections(this WorkflowExecutionContext workflowExecutionContext, IActivity activity) => workflowExecutionContext.Connections.Where(x => x.Target.Activity == activity);
        public static IEnumerable<Connection> GetOutboundConnections(this WorkflowExecutionContext workflowExecutionContext, IActivity activity) => workflowExecutionContext.Connections.Where(x => x.Source.Activity == activity);
        public static IEnumerable<string> GetInboundActivityPath(this WorkflowExecutionContext workflowExecutionContext, IActivity activity) => workflowExecutionContext.GetInboundActivityPathInternal(activity, activity).Distinct();

        private static IEnumerable<string> GetInboundActivityPathInternal(this WorkflowExecutionContext workflowExecutionContext, IActivity activity, IActivity startingPointActivity)
        {
            foreach (var connection in workflowExecutionContext.GetInboundConnections(activity))
            {
                // Circuit breaker: Detect workflows that implement repeating flows to prevent an infinite loop here.
                if (connection.Source.Activity == startingPointActivity)
                    yield break;

                yield return connection.Source.Activity.Id;

                foreach (var parentActivityId in workflowExecutionContext
                    .GetInboundActivityPathInternal(connection.Source.Activity, startingPointActivity)
                    .Distinct())
                    yield return parentActivityId;
            }
        }
    }
}