using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Services.Models
{
    public class ActivityExecutionContext
    {
        public ActivityExecutionContext(WorkflowExecutionContext workflowExecutionContext, IActivity activity, Variable? input = null)
        {
            WorkflowExecutionContext = workflowExecutionContext;
            Activity = activity;
            Input = input;
            Outcomes = new List<string>(0);
        }

        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public IActivity Activity { get; }
        public Variable? Input { get; }
        public Variable? Output { get; set; }
        public IReadOnlyCollection<string> Outcomes { get; set; }

        public Task<T> EvaluateAsync<T>(IWorkflowExpression<T> expression, CancellationToken cancellationToken) =>
            WorkflowExecutionContext.EvaluateAsync(expression, this, cancellationToken);
        
        public Task<object> EvaluateAsync(IWorkflowExpression expression, CancellationToken cancellationToken) =>
            WorkflowExecutionContext.EvaluateAsync(expression, this, cancellationToken);

        public void SetVariable(string name, object value) => WorkflowExecutionContext.SetVariable(name, value);
    }
}