using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa
{
    public static class WorkflowExecutionContextExtensions
    {
        public static IEnumerable<IConnection> GetInboundConnections(this WorkflowExecutionContext workflowExecutionContext, string activityId) =>
            workflowExecutionContext.WorkflowBlueprint.Connections.Where(x => x.Target.Activity.Id == activityId);

        public static IEnumerable<IConnection> GetOutboundConnections(this WorkflowExecutionContext workflowExecutionContext, string activityId) =>
            workflowExecutionContext.WorkflowBlueprint.Connections.Where(x => x.Source.Activity.Id == activityId);

        public static IEnumerable<string> GetInboundActivityPath(this WorkflowExecutionContext workflowExecutionContext, string activityId) => workflowExecutionContext.GetInboundActivityPathInternal(activityId, activityId).Distinct();
        public static IEnumerable<string> GetOutboundActivityPath(this WorkflowExecutionContext workflowExecutionContext, string activityId) => workflowExecutionContext.GetOutboundActivityPathInternal(activityId, activityId).Distinct();
        public static async ValueTask<T?> GetOutputFromAsync<T>(this WorkflowExecutionContext workflowExecutionContext, string activityName, CancellationToken cancellationToken = default) => await workflowExecutionContext.GetOutputFromAsync<T>(activityName, cancellationToken);

        private static IEnumerable<string> GetInboundActivityPathInternal(this WorkflowExecutionContext workflowExecutionContext, string activityId, string startingPointActivityId)
        {
            foreach (var connection in workflowExecutionContext.GetInboundConnections(activityId))
            {
                // Circuit breaker: Detect workflows that implement repeating flows to prevent an infinite loop here.
                if (connection.Source.Activity.Id == startingPointActivityId)
                    yield break;

                yield return connection.Source.Activity.Id!;

                foreach (var parentActivityId in workflowExecutionContext
                    .GetInboundActivityPathInternal(connection.Source.Activity.Id!, startingPointActivityId)
                    .Distinct())
                    yield return parentActivityId;
            }
        }
        
        private static IEnumerable<string> GetOutboundActivityPathInternal(this WorkflowExecutionContext workflowExecutionContext, string activityId, string startingPointActivityId)
        {
            foreach (var connection in workflowExecutionContext.GetOutboundConnections(activityId))
            {
                // Circuit breaker: Detect workflows that implement repeating flows to prevent an infinite loop here.
                if (connection.Target.Activity.Id == startingPointActivityId)
                    yield break;

                yield return connection.Target.Activity.Id!;

                foreach (var childActivityId in workflowExecutionContext
                    .GetOutboundActivityPathInternal(connection.Target.Activity.Id!, startingPointActivityId)
                    .Distinct())
                    yield return childActivityId;
            }
        }
    }
}