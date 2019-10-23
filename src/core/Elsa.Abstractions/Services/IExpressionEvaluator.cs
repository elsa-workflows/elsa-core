using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IExpressionEvaluator
    {
        string Syntax { get; }
        Task<object> EvaluateAsync(string expression, Type type, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken);
    }
}