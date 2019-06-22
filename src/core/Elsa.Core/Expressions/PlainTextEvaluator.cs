using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Expressions
{
    public class PlainTextEvaluator : IExpressionEvaluator
    {
        public static WorkflowExpression<T> Expression<T>(string expression)
        {
            return new WorkflowExpression<T>(SyntaxName, expression);
        }
        
        public const string SyntaxName = "PlainText";
        public string Syntax => SyntaxName;

        public Task<T> EvaluateAsync<T>(string expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return Task.FromResult(default(T));
            
            return Task.FromResult((T) Convert.ChangeType(expression, typeof(T)));
        }
    }
}