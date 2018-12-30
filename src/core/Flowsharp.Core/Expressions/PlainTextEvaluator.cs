using System;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp.Expressions
{
    public class PlainTextEvaluator : IExpressionEvaluator
    {
        public const string SyntaxName = "PlainText";
        public string Syntax => SyntaxName;
        
        public Task<T> EvaluateAsync<T>(string expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            return Task.FromResult((T) Convert.ChangeType(expression, typeof(T)));
        }
    }
}