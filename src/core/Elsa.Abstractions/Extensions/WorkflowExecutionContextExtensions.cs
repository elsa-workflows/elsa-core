using System.Collections.Generic;
using System.Linq;
using Elsa.Services.Models;

namespace Elsa
{
    public static class WorkflowExecutionContextExtensions
    {
        public static IEnumerable<IConnection> GetInboundConnections(this WorkflowExecutionContext workflowExecutionContext, string activityId) =>
            workflowExecutionContext.WorkflowBlueprint.Connections.Where(x => x.Target.Activity.Id == activityId);

        public static IEnumerable<IConnection> GetInboundConnectionPath(this WorkflowExecutionContext workflowExecutionContext, string activityId) => workflowExecutionContext.GetInboundConnectionPath(activityId, activityId);

        public static IEnumerable<IConnection> GetOutboundConnections(this WorkflowExecutionContext workflowExecutionContext, string activityId) =>
            workflowExecutionContext.WorkflowBlueprint.Connections.Where(x => x.Source.Activity.Id == activityId);

        public static IEnumerable<string> GetInboundActivityPath(this WorkflowExecutionContext workflowExecutionContext, string activityId) => workflowExecutionContext.WorkflowBlueprint.GetInboundActivityPath(activityId).Distinct();

        private static IEnumerable<IConnection> GetInboundConnectionPath(this WorkflowExecutionContext workflowExecutionContext, string activityId, string startingPointActivityId)
        {
            foreach (var connection in workflowExecutionContext.GetInboundConnections(activityId))
            {
                // Circuit breaker: Detect workflows that implement repeating flows to prevent an infinite loop here.
                if (connection.Source.Activity.Id == startingPointActivityId)
                    yield break;

                yield return connection;

                foreach (var parentConnection in workflowExecutionContext
                    .GetInboundConnectionPath(connection.Source.Activity.Id!, startingPointActivityId)
                    .Distinct())
                    yield return parentConnection;
            }
        }
    }
}