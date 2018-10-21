using Flowsharp.Expressions;

namespace Flowsharp.Activities.Primitives.Activities
{
    public class IfElse : Activity
    {
        public WorkflowExpression<bool> ConditionExpression { get; set; }
    }
}