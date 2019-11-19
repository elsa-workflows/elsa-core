using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public class LiteralEvaluator : IExpressionEvaluator
    {
        public static WorkflowExpression<T> Expression<T>(string expression)
        {
            return new WorkflowExpression<T>(SyntaxName, expression);
        }
        
        public const string SyntaxName = "Literal";
        public string Syntax => SyntaxName;

        public Task<object> EvaluateAsync(string expression, Type type, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                workflowExecutionContext.SetLastExpressionResult(default(object));
                return Task.FromResult(default(object));
            }

            object convertedResult = expression.Parse(type);
            workflowExecutionContext.SetLastExpressionResult(convertedResult);
            return Task.FromResult(convertedResult);
        }
    }
}