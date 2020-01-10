using System;
using System.Runtime.CompilerServices;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public class CodeExpression : WorkflowExpression
    {
        public static string ExpressionType => "Code";

        public CodeExpression(Func<ActivityExecutionContext, object> expression, Type returnType) : base(ExpressionType, returnType)
        {
            Expression = expression;
        }
        
        public CodeExpression(Func<object> expression, Type returnType) : base(ExpressionType, returnType)
        {
            Expression = context => expression();
        }

        public Func<ActivityExecutionContext, object> Expression { get; }
    }

    public class CodeExpression<T> : CodeExpression, IWorkflowExpression<T>
    {
        public CodeExpression(Func<ActivityExecutionContext, T> expression) : base(context => expression(context), typeof(T))
        {
        }
        
        public CodeExpression(Func<T> expression) : base(context => expression(), typeof(T))
        {
        }
    }
}