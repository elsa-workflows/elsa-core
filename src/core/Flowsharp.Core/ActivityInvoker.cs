using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Results;
using Microsoft.Extensions.Localization;

namespace Flowsharp
{
    public class ActivityInvoker : IActivityInvoker
    {
        private readonly IDictionary<Type, IActivityHandler> handlers;

        public ActivityInvoker(IEnumerable<IActivityHandler> handlers)
        {
            this.handlers = handlers.ToDictionary(x => x.ActivityType);
        }

        public async Task<bool> CanExecuteAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => await GetHandlerFor(activity).CanExecuteAsync(activity, workflowContext, cancellationToken);
        public IEnumerable<LocalizedString> GetEndpoints(IActivity activity) => GetHandlerFor(activity).GetEndpoints();
        public async Task<ActivityExecutionResult> ExecuteAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => await GetHandlerFor(activity).ExecuteAsync(activity, workflowContext, cancellationToken);
        public async Task<ActivityExecutionResult> ResumeAsync(IActivity activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken) => await GetHandlerFor(activity).ResumeAsync(activity, workflowContext, cancellationToken);
        private IActivityHandler GetHandlerFor(IActivity activity) => handlers[activity.GetType()];
    }
}