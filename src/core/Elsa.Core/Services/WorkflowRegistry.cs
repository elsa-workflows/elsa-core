using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Caching;
using Elsa.Extensions;
using Elsa.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services
{
    public class WorkflowRegistry : IWorkflowRegistry
    {
        internal const string CacheKey = "elsa:workflow-registry";
        private readonly IServiceProvider serviceProvider;
        private readonly IMemoryCache cache;
        private readonly ISignal signal;

        public WorkflowRegistry(
            IMemoryCache cache,
            ISignal signal,
            IServiceProvider serviceProvider)
        {
            this.cache = cache;
            this.signal = signal;
            this.serviceProvider = serviceProvider;
        }

        public async Task<IEnumerable<(WorkflowDefinitionVersion, ActivityDefinition)>> ListByStartActivityAsync(
            string activityType,
            CancellationToken cancellationToken)
        {
            var workflowDefinitions = await ReadCacheAsync(cancellationToken);

            var query =
                from workflow in workflowDefinitions
                where workflow.IsPublished
                from activity in workflow.GetStartActivities()
                where activity.Type == activityType
                select (workflow, activity);

            return query.Distinct();
        }

        public async Task<WorkflowDefinitionVersion> GetWorkflowDefinitionAsync(
            string id,
            VersionOptions version,
            CancellationToken cancellationToken)
        {
            var workflowDefinitions = await ReadCacheAsync(cancellationToken);

            return workflowDefinitions
                .Where(x => x.DefinitionId == id)
                .OrderByDescending(x => x.Version)
                .WithVersion(version).FirstOrDefault();
        }

        private async Task<ICollection<WorkflowDefinitionVersion>> ReadCacheAsync(CancellationToken cancellationToken)
        {
            return await cache.GetOrCreateAsync(
                CacheKey,
                async entry =>
                {
                    var workflowDefinitions = await LoadWorkflowDefinitionsAsync(cancellationToken);

                    entry.SlidingExpiration = TimeSpan.FromHours(1);
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4);
                    entry.Monitor(signal.GetToken(CacheKey));
                    return workflowDefinitions;
                });
        }

        private async Task<ICollection<WorkflowDefinitionVersion>> LoadWorkflowDefinitionsAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();
            var providers = scope.ServiceProvider.GetServices<IWorkflowProvider>();
            var tasks = await Task.WhenAll(providers.Select(x => x.GetWorkflowDefinitionsAsync(cancellationToken)));
            return tasks.SelectMany(x => x).ToList();
        }
    }
}