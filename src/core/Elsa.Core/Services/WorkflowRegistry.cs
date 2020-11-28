using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

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
            string? tenantId,
            VersionOptions version,
            CancellationToken cancellationToken)
        {
            var workflows = await GetWorkflowsAsync(cancellationToken).ToListAsync(cancellationToken);
            var query = workflows.Where(workflow => workflow.Id == id && workflow.WithVersion(version));

            if (tenantId != null)
                query = query.Where(x => x.TenantId == tenantId);

            return query
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();
        }

        public async Task<IEnumerable<IWorkflowBlueprint>> FindWorkflowsAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken) =>
            await GetWorkflowsAsync(cancellationToken).Where(predicate).OrderByDescending(x => x.Version).ToListAsync(cancellationToken);

        public async Task<IWorkflowBlueprint?> FindWorkflowAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken) => 
            await GetWorkflowsAsync(cancellationToken).Where(predicate).OrderByDescending(x => x.Version).FirstOrDefaultAsync(cancellationToken);
    }
}