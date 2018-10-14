using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Samples.Console.Workflows;
using Flowsharp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowsharp.Samples.Console.Programs
{
    public class AdditionWorkflowProgramLongRunning
    {
        private readonly WorkflowInvoker workflowInvoker;

        public AdditionWorkflowProgramLongRunning()
        {
            workflowInvoker = new WorkflowInvoker(new Logger<WorkflowInvoker>(new NullLoggerFactory()));
        }
        
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var workflow = new AdditionWorkflowLongRunning();
            var workflowContext = await workflowInvoker.InvokeAsync(workflow, null, cancellationToken);

            while (workflowContext.Workflow.Status == WorkflowStatus.Halted)
            {
                workflowContext = await ReadAndResumeAsync(workflowContext, "x", cancellationToken);
                workflowContext = await ReadAndResumeAsync(workflowContext, "y", cancellationToken);
                workflowContext = await ReadAndResumeAsync(workflowContext, "tryAgain", cancellationToken);    
            }
        }

        private async Task<WorkflowExecutionContext> ReadAndResumeAsync(WorkflowExecutionContext workflowContext, string argumentName, CancellationToken cancellationToken)
        {
            var workflow = workflowContext.Workflow;
            var haltedActivity = workflowContext.Workflow.HaltedActivities.Single();
            workflow.Arguments[argumentName] = System.Console.ReadLine();
            workflow.Status = WorkflowStatus.Resuming;
            return await workflowInvoker.InvokeAsync(workflow, haltedActivity, cancellationToken);
        }
    }
}