using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.Primitives.Activities
{
    [Category("Primitives")]
    [DisplayName("Set Variable")]
    [Description("Set a custom variable on the workflow.")]
    [DefaultEndpoint]
    public class SetVariable : Activity
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