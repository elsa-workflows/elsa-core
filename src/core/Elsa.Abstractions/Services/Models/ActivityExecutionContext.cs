using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Models;
using Microsoft.Extensions.DependencyInjection;

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
        
        public Task<object> EvaluateAsync(IWorkflowExpression expression, Type targetType, CancellationToken cancellationToken) =>
            WorkflowExecutionContext.EvaluateAsync(expression, targetType, this, cancellationToken);
        
        public Task<object> EvaluateAsync(IWorkflowExpression expression, CancellationToken cancellationToken) =>
            WorkflowExecutionContext.EvaluateAsync(expression, typeof(object), this, cancellationToken);

        public void SetVariable(string name, object value) => WorkflowExecutionContext.SetVariable(name, value);
        public object GetVariable(string name) => WorkflowExecutionContext.GetVariable(name);
        public T GetVariable<T>(string name) => WorkflowExecutionContext.GetVariable<T>(name);
        public T GetService<T>() => WorkflowExecutionContext.ServiceProvider.GetService<T>();
    }
}