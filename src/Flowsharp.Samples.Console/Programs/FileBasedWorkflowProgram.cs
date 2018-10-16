using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Services;

namespace Flowsharp.Samples.Console.Programs
{
    public class FileBasedWorkflowProgramLongRunning
    {
        private readonly IWorkflowInvoker workflowInvoker;
        private readonly IWorkflowSerializer serializer;

        public FileBasedWorkflowProgramLongRunning(IWorkflowInvoker workflowInvoker, IWorkflowSerializer serializer)
        {
            this.workflowInvoker = workflowInvoker;
            this.serializer = serializer;
        }
        
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var assembly = typeof(FileBasedWorkflowProgramLongRunning).Assembly;
            var resource = assembly.GetManifestResourceStream("Flowsharp.Samples.Console.SampleWorkflow.yaml");
            var resourceReader = new StreamReader(resource);
            var data = await resourceReader.ReadToEndAsync();
            var workflow = serializer.Deserialize(data);
            var workflowContext = await workflowInvoker.InvokeAsync(workflow, null, cancellationToken);

            while (workflowContext.Workflow.Status == WorkflowStatus.Halted)
            {
                workflowContext = await ReadAndResumeAsync(workflowContext.Workflow, "x", cancellationToken);
                workflowContext = await ReadAndResumeAsync(workflowContext.Workflow, "y", cancellationToken);
                workflowContext = await ReadAndResumeAsync(workflowContext.Workflow, "tryAgain", cancellationToken);    
            }
            
            System.Console.WriteLine(data);
        }

        private async Task<WorkflowExecutionContext> ReadAndResumeAsync(Workflow workflow, string argumentName, CancellationToken cancellationToken)
        {
            var haltedActivity = workflow.HaltedActivities.Single();
            workflow.Arguments[argumentName] = System.Console.ReadLine();
            workflow.Status = WorkflowStatus.Resuming;
            return await workflowInvoker.InvokeAsync(workflow, haltedActivity, cancellationToken);
        }
    }
}