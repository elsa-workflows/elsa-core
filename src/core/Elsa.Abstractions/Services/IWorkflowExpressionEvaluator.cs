using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowExpressionEvaluator
    {
        Task<object> EvaluateAsync(IWorkflowExpression expression, Type type, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken);
    }
}