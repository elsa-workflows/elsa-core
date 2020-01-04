using System;
using System.Runtime.CompilerServices;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public class CodeExpression : WorkflowExpression
    {
        public static string ExpressionType => "Code";

        public CodeExpression(Func<WorkflowExecutionContext, ActivityExecutionContext, object> expression, Type returnType) : base(ExpressionType, returnType)
        {
            Expression = expression;
        }
        
        public CodeExpression(Func<object> expression, Type returnType) : base(ExpressionType, returnType)
        {
            Expression = (w, a) => expression();
        }

        public Func<WorkflowExecutionContext, ActivityExecutionContext, object> Expression { get; }
    }

    public class CodeExpression<T> : CodeExpression, IWorkflowExpression<T>
    {
        public CodeExpression(Func<WorkflowExecutionContext, ActivityExecutionContext, T> expression) : base((x, y) => expression(x, y), typeof(T))
        {
        }
        
        public CodeExpression(Func<T> expression) : base((x, y) => expression(), typeof(T))
        {
        }
    }
}