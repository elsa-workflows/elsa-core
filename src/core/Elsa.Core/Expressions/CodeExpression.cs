using System;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public class CodeExpression : WorkflowExpression
    {
        public static string ExpressionType => "Code";

        public CodeExpression(Func<ActivityExecutionContext, object> expression) : base(ExpressionType)
        {
            Expression = expression;
        }
        
        public CodeExpression(Func<object> expression) : base(ExpressionType)
        {
            Expression = context => expression();
        }

        public Func<ActivityExecutionContext, object> Expression { get; }
    }

    public class CodeExpression<T> : CodeExpression, IWorkflowExpression<T>
    {
        public CodeExpression(Func<ActivityExecutionContext, T> expression) : base(context => expression != null ? expression(context) : default)
        {
        }
        
        public CodeExpression(Func<T> expression) : base(context => expression != null ? expression() : default)
        {
        }
        
        public CodeExpression(T value) : base(() => value)
        {
        }
    }
}