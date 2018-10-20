using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Samples.Console.Activities;
using Flowsharp.Samples.Console.Workflows;
using Flowsharp.Serialization;

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
            var workflowContext = await workflowInvoker.InvokeAsync(workflow, null, Variables.Empty, cancellationToken);

            while (workflowContext.Workflow.Status == WorkflowStatus.Halted)
            {
                workflowContext = await ReadAndResumeAsync(workflowContext.Workflow, cancellationToken);
                workflowContext = await ReadAndResumeAsync(workflowContext.Workflow, cancellationToken);
                workflowContext = await ReadAndResumeAsync(workflowContext.Workflow, cancellationToken);    
            }
            
            var json = serializer.Serialize(workflowContext.Workflow);
            System.Console.WriteLine(json);
        }

        private async Task<WorkflowExecutionContext> ReadAndResumeAsync(Workflow workflow, CancellationToken cancellationToken)
        {
            var json = serializer.Serialize(workflow);
            workflow = serializer.Deserialize(json);
            var haltedActivity = (ReadLine)workflow.BlockingActivities.Single();
            var args = new Variables {{ haltedActivity.ArgumentName, System.Console.ReadLine() }};
            
            return await workflowInvoker.ResumeAsync(workflow, haltedActivity, args, cancellationToken);
        }
    }
}