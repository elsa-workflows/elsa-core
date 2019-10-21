using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services
{
    public class WorkflowRegistry : IWorkflowRegistry
    {
        private const string CacheKey = "elsa:workflow-registry";
        private readonly IServiceProvider serviceProvider;
        private readonly IMemoryCache cache;

        public WorkflowRegistry(
            IServiceProvider serviceProvider,
            IMemoryCache cache)
        {
            this.serviceProvider = serviceProvider;
            this.cache = cache;
        }

        public async Task<IEnumerable<(WorkflowDefinitionVersion, ActivityDefinition)>> ListByStartActivityAsync(
            string activityType,
            CancellationToken cancellationToken)
        {
            var workflowDefinitions = await ReadCacheAsync(cancellationToken);

            var query =
                from workflow in workflowDefinitions
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

        public Task EvictAsync(CancellationToken cancellationToken)
        {
            cache.Remove(CacheKey);
            return Task.CompletedTask;
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
                    return workflowDefinitions;
                });
        }

        private async Task<ICollection<WorkflowDefinitionVersion>> LoadWorkflowDefinitionsAsync(
            CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();
            var providers = scope.ServiceProvider.GetServices<IWorkflowProvider>();
            var tasks = await Task.WhenAll(providers.Select(x => x.GetWorkflowDefinitionsAsync(cancellationToken)));
            return tasks.SelectMany(x => x).ToList();
        }
    }
}