using System;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public class VariableExpression : WorkflowExpression
    {
        public static string ExpressionType => "Variable";

        public VariableExpression(string variableName, Type returnType) : base(ExpressionType, returnType)
        {
            VariableName = variableName;
        }

        public string VariableName { get; }
    }

    public class VariableExpression<T> : VariableExpression, IWorkflowExpression<T>
    {
        public VariableExpression(string variableName) : base(variableName, typeof(T))
        {
        }
    }
}