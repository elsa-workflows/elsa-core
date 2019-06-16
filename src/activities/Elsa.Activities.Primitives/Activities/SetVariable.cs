using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.Primitives.Activities
{
    public class SetVariable : ActivityBase
    {
        public SetVariable()
        {
        }

        public SetVariable(string variableName, string valueExpressionSyntax, string valueExpression)
        {
            VariableName = variableName;
            ValueExpression = new WorkflowExpression<object>(valueExpressionSyntax, valueExpression);
        }
        
        public string VariableName { get; set; }
        public WorkflowExpression<object> ValueExpression { get; set; }
    }
}