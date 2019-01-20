using Elsa.Expressions;

namespace Elsa.Web.Components.ViewModels
{
    public class ExpressionViewModel
    {
        public ExpressionViewModel()
        {
        }

        public ExpressionViewModel(string expression, string syntax)
        {
            Expression = expression;
            Syntax = syntax;
        }

        public ExpressionViewModel(WorkflowExpression workflowExpression)
            : this(workflowExpression?.Expression, workflowExpression?.Syntax)
        {
        }

        public string Expression { get; set; }
        public string Syntax { get; set; }
        public WorkflowExpression<T> ToWorkflowExpression<T>() => new WorkflowExpression<T>(Syntax, Expression);
    }
}