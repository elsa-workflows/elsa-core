﻿using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public interface IWorkflowExpressionHandler
    {
        string Type { get; }
        Task<object> EvaluateAsync(
            IWorkflowExpression expression,
            ActivityExecutionContext context,
            CancellationToken cancellationToken);
    }
}