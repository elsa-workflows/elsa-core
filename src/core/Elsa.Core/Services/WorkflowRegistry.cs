using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public async IAsyncEnumerable<IWorkflowBlueprint> GetWorkflowsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var providers = _workflowProviders;

            foreach (var provider in providers)
            await foreach (var workflow in provider.GetWorkflowsAsync(cancellationToken).WithCancellation(cancellationToken))
                yield return workflow;
        }

        public async Task<IWorkflowBlueprint?> GetWorkflowAsync(
            string id,
            VersionOptions version,
            CancellationToken cancellationToken)
        {
            var workflows = await GetWorkflowsAsync(cancellationToken).ToListAsync(cancellationToken);

            return workflows
                .Where(x => x.Id == id)
                .OrderByDescending(x => x.Version)
                .WithVersion(version)
                .FirstOrDefault();
        }
    }
}