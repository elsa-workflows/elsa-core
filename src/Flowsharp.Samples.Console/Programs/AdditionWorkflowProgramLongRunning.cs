using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.Expressions;
using Flowsharp.Handlers;
using Flowsharp.Models;
using Flowsharp.Samples.Console.Handlers;
using Flowsharp.Samples.Console.Workflows;
using Flowsharp.Serialization;
using Flowsharp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowsharp.Samples.Console.Programs
{
    public class AdditionWorkflowProgramLongRunning
    {
        private readonly IWorkflowInvoker workflowInvoker;
        private readonly IWorkflowSerializer serializer;

        public AdditionWorkflowProgramLongRunning(IWorkflowInvoker workflowInvoker, IWorkflowSerializer serializer)
        {
            this.workflowInvoker = workflowInvoker;
            this.serializer = serializer;
        }
        
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var workflow = new AdditionWorkflowLongRunning();
            var workflowContext = await workflowInvoker.InvokeAsync(workflow, null, cancellationToken);

            while (workflowContext.Workflow.Status == WorkflowStatus.Halted)
            {
                workflowContext = await ReadAndResumeAsync(workflowContext.Workflow, "x", cancellationToken);
                workflowContext = await ReadAndResumeAsync(workflowContext.Workflow, "y", cancellationToken);
                workflowContext = await ReadAndResumeAsync(workflowContext.Workflow, "tryAgain", cancellationToken);    
            }
            
            var json = await serializer.SerializeAsync(workflowContext.Workflow, cancellationToken);
            System.Console.WriteLine(json);
        }

        private async Task<WorkflowExecutionContext> ReadAndResumeAsync(Workflow workflow, string argumentName, CancellationToken cancellationToken)
        {
            var json = await serializer.SerializeAsync(workflow, cancellationToken);
            workflow = await serializer.DeserializeAsync(json, cancellationToken);
            var haltedActivity = workflow.HaltedActivities.Single();
            workflow.Arguments[argumentName] = System.Console.ReadLine();
            workflow.Status = WorkflowStatus.Resuming;
            return await workflowInvoker.InvokeAsync(workflow, haltedActivity, cancellationToken);
        }
    }
}