using System;
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

        public async Task<IEnumerable<IWorkflowBlueprint>> ListAsync(CancellationToken cancellationToken) => await GetWorkflowsInternalAsync(cancellationToken).ToListAsync(cancellationToken);

        public async Task<IWorkflowBlueprint?> GetAsync(string id, string? tenantId, VersionOptions version, CancellationToken cancellationToken) =>
            await FindAsync(x => x.Id == id && x.TenantId == tenantId && x.WithVersion(version), cancellationToken);

        public async Task<IEnumerable<IWorkflowBlueprint>> FindManyAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken) =>
            (await ListAsync(cancellationToken).Where(predicate).OrderByDescending(x => x.Version)).ToList();

        public async Task<IWorkflowBlueprint?> FindAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken) =>
            (await ListAsync(cancellationToken).Where(predicate).OrderByDescending(x => x.Version)).FirstOrDefault();

        private async IAsyncEnumerable<IWorkflowBlueprint> GetWorkflowsInternalAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var providers = _workflowProviders;

            foreach (var provider in providers)
            await foreach (var workflow in provider.GetWorkflowsAsync(cancellationToken).WithCancellation(cancellationToken))
                yield return workflow;
        }
    }
}