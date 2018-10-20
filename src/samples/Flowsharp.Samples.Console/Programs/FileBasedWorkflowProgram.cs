using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Samples.Console.Activities;
using Flowsharp.Serialization;

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
            var workflowContext = await workflowInvoker.InvokeAsync(workflow, null, Variables.Empty, cancellationToken);

            while (workflowContext.Workflow.Status == WorkflowStatus.Halted)
            {
                workflowContext = await ReadAndResumeAsync(workflowContext.Workflow, cancellationToken);
                workflowContext = await ReadAndResumeAsync(workflowContext.Workflow, cancellationToken);
                workflowContext = await ReadAndResumeAsync(workflowContext.Workflow, cancellationToken);    
            }
            
            System.Console.WriteLine(data);
        }

        private async Task<WorkflowExecutionContext> ReadAndResumeAsync(Workflow workflow, CancellationToken cancellationToken)
        {
            var haltedActivity = (ReadLine)workflow.BlockingActivities.Single();
            var args = new Variables {{ haltedActivity.ArgumentName, System.Console.ReadLine() }};
            return await workflowInvoker.ResumeAsync(workflow, haltedActivity, args, cancellationToken);
        }
    }
}