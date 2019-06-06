using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.Primitives.Activities
{
    [ActivityCategory("Control Flow")]
    [ActivityDisplayName("If/Else")]
    [ActivityDescription("Evaluate a boolean condition and continues execution based on the outcome.")]
    [Endpoints(EndpointNames.True, EndpointNames.False)]
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