using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Services;

namespace Elsa.Activities.Startup.HostedServices
{
    public class RunStartupWorkflows : IStartupTask
    {
        private readonly IWorkflowRunner _workflowRunner;

        public RunStartupWorkflows(IWorkflowRunner workflowRunner)
        {
            _workflowRunner = workflowRunner;
        }

        public int Order => 1000;

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await _workflowRunner.TriggerWorkflowsAsync<Activities.Startup>(NullBookmark.Instance, null, null, null, null, cancellationToken);
        }
    }
}