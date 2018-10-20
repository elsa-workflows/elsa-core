using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Persistence;
using Flowsharp.Persistence.Models;
using Flowsharp.Runtime.Abstractions;
using Flowsharp.Serialization;

namespace Flowsharp.Samples.Console.Programs
{
    public class WorkflowHostProgram
    {
        private readonly IWorkflowHost workflowHost;
        private readonly IWorkflowSerializer serializer;
        private readonly IWorkflowDefinitionStore workflowStore;

        public WorkflowHostProgram(IWorkflowHost workflowHost, IWorkflowSerializer serializer, IWorkflowDefinitionStore workflowStore)
        {
            this.workflowHost = workflowHost;
            this.serializer = serializer;
            this.workflowStore = workflowStore;
        }
        
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var assembly = typeof(FileBasedWorkflowProgramLongRunning).Assembly;
            var resource = assembly.GetManifestResourceStream("Flowsharp.Samples.Console.SampleWorkflow.yaml");
            var resourceReader = new StreamReader(resource);
            var data = await resourceReader.ReadToEndAsync();
            var workflow = serializer.Deserialize(data);
            var workflowDefinition = new WorkflowDefinition { Id = "1", Workflow = workflow};

            await workflowStore.AddAsync(workflowDefinition, cancellationToken);
            await workflowHost.TriggerWorkflowAsync("WriteLine", Variables.Empty, cancellationToken);
            await ReadAndResumeAsync("x", cancellationToken);
            await ReadAndResumeAsync("y", cancellationToken);
            await ReadAndResumeAsync("tryAgain", cancellationToken);
            
            System.Console.WriteLine(data);
        }

        private async Task ReadAndResumeAsync(string argName, CancellationToken cancellationToken)
        {
            var args = new Variables { {argName, System.Console.ReadLine()} };
            await workflowHost.TriggerWorkflowAsync("ReadLine", args, cancellationToken);
        }
    }
}