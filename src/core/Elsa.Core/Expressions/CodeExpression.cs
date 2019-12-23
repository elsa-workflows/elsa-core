using System;
using Elsa.Services.Models;
using Newtonsoft.Json;

namespace Elsa.Expressions
{
    public class CodeExpression : WorkflowExpression
    {
        public static string ExpressionType => "Code";
        
        public CodeExpression(Func<WorkflowExecutionContext, object> expression, Type returnType) : base(ExpressionType, returnType)
        {
            Expression = expression;
        }

        //[JsonIgnore]
        public Func<WorkflowExecutionContext, object> Expression { get; }
    }
    
    public class CodeExpression<T> : CodeExpression, IWorkflowExpression<T>
    {
        public CodeExpression(Func<WorkflowExecutionContext, T> expression) : base(context => expression(context), typeof(T))
        {
        }
    }
}