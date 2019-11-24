using Elsa.Models;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class WorkflowExecutionScopeType : ObjectGraphType<WorkflowExecutionScope>
    {
        public WorkflowExecutionScopeType()
        {
            Name = "WorkflowExecutionScope";

            Field(x => x.Variables, type: typeof(ListGraphType<VariableType>)).Description("The variables within this scope.");
        }
    }
}