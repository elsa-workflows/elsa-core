using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Results;
using Microsoft.Extensions.Logging;

namespace Elsa
{
    public class ActivityInvoker : IActivityInvoker
    {
        private readonly IActivityDriverRegistry driverRegistry;

        public ActivityInvoker(IActivityDriverRegistry driverRegistry)
        {
            this.driverRegistry = driverRegistry;
        }

        public async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default)
        {
            return await InvokeAsync(workflowContext, activity, (context, driver) => driver.ExecuteAsync(context, workflowContext, cancellationToken));
        }

        public async Task<ActivityExecutionResult> ResumeAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default)
        {
            return await InvokeAsync(workflowContext, activity, (context, driver) => driver.ResumeAsync(context, workflowContext, cancellationToken));
        }

        private Task<ActivityExecutionResult> InvokeAsync(
            WorkflowExecutionContext workflowContext,
            IActivity activity,
            Func<ActivityExecutionContext, IActivityDriver, Task<ActivityExecutionResult>> invokeAction)
        {
            var activityContext = workflowContext.CreateActivityExecutionContext(activity);
            var driver = driverRegistry.GetDriver(activity.Name);
            return invokeAction(activityContext, driver);
        }
    }
}