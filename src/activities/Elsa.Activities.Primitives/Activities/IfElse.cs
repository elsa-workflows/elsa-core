using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.Primitives.Activities
{
    public class IfElse : Activity
    {
        public IfElse()
        {
        }

        public IfElse(WorkflowExpression<bool> conditionExpression)
        {
            ConditionExpression = conditionExpression;
        }
        
        public WorkflowExpression<bool> ConditionExpression { get; set; }
    }
}