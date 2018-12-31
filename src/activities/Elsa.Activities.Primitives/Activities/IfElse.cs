using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.Primitives.Activities
{
    public class IfElse : Activity
    {
        public WorkflowExpression<bool> ConditionExpression { get; set; }
    }
}