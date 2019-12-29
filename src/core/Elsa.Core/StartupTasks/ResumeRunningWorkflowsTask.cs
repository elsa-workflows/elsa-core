using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Runtime;
using Elsa.Services;

namespace Elsa.StartupTasks
{
    /// <summary>
    /// If there are workflows in the Running state while the server starts, it means the workflow instance never finished execution, e.g. because the workflow host terminated.
    /// This startup task resumes such workflows.
    /// </summary>
    // TODO: How would this work when adding multi-node support? E.g. we don't want new nodes to start running workflows while another node is already running them.
    public class ResumeRunningWorkflowsTask : IStartupTask
    {
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly IProcessRunner processRunner;

        public ResumeRunningWorkflowsTask(IWorkflowInstanceStore workflowInstanceStore, IProcessRunner processRunner)
        {
            this.workflowInstanceStore = workflowInstanceStore;
            this.processRunner = processRunner;
        }
        
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var instances = await workflowInstanceStore.ListByStatusAsync(ProcessStatus.Running, cancellationToken);

            foreach (var instance in instances)
            {
                //await processRunner.RunAsync(instance, cancellationToken: cancellationToken);
            }
        }
    }
}