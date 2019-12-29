using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public abstract class WorkflowEventHandlerBase : IWorkflowEventHandler
    {
        public virtual Task ActivityExecutedAsync(
            ProcessExecutionContext processExecutionContext,
            IActivity activity,
            CancellationToken cancellationToken)
        {
            ActivityExecuted(processExecutionContext, activity);
            return Task.CompletedTask;
        }

        public virtual Task ActivityFaultedAsync(
            ProcessExecutionContext processExecutionContext,
            IActivity activity,
            string message,
            CancellationToken cancellationToken)
        {
            ActivityFaulted(processExecutionContext, activity, message);
            return Task.CompletedTask;
        }

        public virtual Task InvokingHaltedActivitiesAsync(
            ProcessExecutionContext processExecutionContext,
            CancellationToken cancellationToken)
        {
            InvokingHaltedActivities(processExecutionContext);
            return Task.CompletedTask;
        }

        public virtual Task WorkflowInvokedAsync(
            ProcessExecutionContext processExecutionContext,
            CancellationToken cancellationToken)
        {
            WorkflowInvoked(processExecutionContext);
            return Task.CompletedTask;
        }

        protected virtual void ActivityExecuted(ProcessExecutionContext processExecutionContext, IActivity activity)
        {
        }

        protected virtual void ActivityFaulted(
            ProcessExecutionContext processExecutionContext,
            IActivity activity,
            string message)
        {
        }

        protected virtual void InvokingHaltedActivities(ProcessExecutionContext processExecutionContext)
        {
        }

        protected virtual void WorkflowInvoked(ProcessExecutionContext processExecutionContext)
        {
        }
    }
}