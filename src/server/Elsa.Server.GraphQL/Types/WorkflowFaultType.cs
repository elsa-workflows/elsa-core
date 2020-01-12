using Elsa.Models;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class WorkflowFaultType : ObjectGraphType<ProcessFault>
    {
        public WorkflowFaultType()
        {
            Name = "WorkflowFault";

            Field(x => x.FaultedActivityId).Description("The ID of the activity that faulted.");
            Field(x => x.Message).Description("The fault message.");
        }
    }
}