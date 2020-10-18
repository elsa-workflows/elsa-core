using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;
using Open.Linq.AsyncExtensions;

namespace Elsa.Services
{
    public class WorkflowRegistry : IWorkflowRegistry
    {
        private readonly IEnumerable<IWorkflowProvider> _workflowProviders;

        public WorkflowRegistry(IEnumerable<IWorkflowProvider> workflowProviders)
        {
            _workflowProviders = workflowProviders;
        }

        public async Task<IEnumerable<IWorkflowBlueprint>> GetWorkflowsAsync(CancellationToken cancellationToken)
        {
            var providers = _workflowProviders;
            var tasks = await Task.WhenAll(providers.Select(x => x.GetWorkflowsAsync(cancellationToken)));
            return tasks.SelectMany(x => x).ToList();
        }

        public async Task<IWorkflowBlueprint?> GetWorkflowAsync(
            string id,
            VersionOptions version,
            CancellationToken cancellationToken)
        {
            var workflows = await GetWorkflowsAsync(cancellationToken).ToList();

            return workflows
                .Where(x => x.Id == id)
                .OrderByDescending(x => x.Version)
                .WithVersion(version)
                .FirstOrDefault();
        }
    }
}