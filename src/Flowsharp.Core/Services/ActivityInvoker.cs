using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Results;

namespace Flowsharp.Services
{
    public class ActivityInvoker : IActivityInvoker
    {
        private readonly IDictionary<Type, IActivityHandler> handlers;

        public ActivityInvoker(IEnumerable<IActivityHandler> handlers)
        {
            this.handlers = handlers.ToDictionary(x => x.ActivityType);
        }

        public async Task<bool> CanExecuteAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            return await GetHandlerFor(activity).CanExecuteAsync(activity, workflowContext, cancellationToken);
        }

        public async Task<ActivityExecutionResult> ExecuteAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            return await GetHandlerFor(activity).ExecuteAsync(activity, workflowContext, cancellationToken);
        }

        public async Task<ActivityExecutionResult> ResumeAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            return await GetHandlerFor(activity).ResumeAsync(activity, workflowContext, cancellationToken);
        }
        
        private IActivityHandler GetHandlerFor(IActivity activity)
        {
            return handlers[activity.GetType()];
        }
    }
}