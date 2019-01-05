using Elsa.Expressions;

namespace Elsa.Web.Components.ViewModels
{
    public class ExpressionViewModel
    {
        public ExpressionViewModel()
        {
        }
        
        public ExpressionViewModel(WorkflowExpression workflowExpression)
        {
            Expression = workflowExpression?.Expression;
            Syntax = workflowExpression?.Syntax;
        }

        public string Expression { get; set; }
        public string Syntax { get; set; }
        public WorkflowExpression<T> ToWorkflowExpression<T>() => new WorkflowExpression<T>(Syntax, Expression);
    }
}